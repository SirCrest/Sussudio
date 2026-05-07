#!/usr/bin/env python3
"""Audit Codex token usage for the Sussudio/ElgatoCapture project family."""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import sqlite3
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


PROJECT_NAME_RE = re.compile(
    r"(Sussudio|ElgatoCapture|ElgatoCapture-Flashback|ElgatoCapture-[A-Za-z0-9_-]+)",
    re.IGNORECASE,
)
CORE_PATH_RE = re.compile(r"(^|[\\/])(Sussudio|ElgatoCapture($|[-\\/]))", re.IGNORECASE)
PLUGIN_PATH_RE = re.compile(r"(^|[\\/])Sussudio-Plugin($|[\\/])", re.IGNORECASE)


@dataclass
class Usage:
    input_tokens: int = 0
    cached_input_tokens: int = 0
    output_tokens: int = 0
    reasoning_output_tokens: int = 0
    total_tokens: int = 0

    @property
    def uncached_input_tokens(self) -> int:
        return max(0, self.input_tokens - self.cached_input_tokens)

    @property
    def visible_output_tokens(self) -> int:
        return max(0, self.output_tokens - self.reasoning_output_tokens)

    def add(self, other: "Usage") -> None:
        self.input_tokens += other.input_tokens
        self.cached_input_tokens += other.cached_input_tokens
        self.output_tokens += other.output_tokens
        self.reasoning_output_tokens += other.reasoning_output_tokens
        self.total_tokens += other.total_tokens


@dataclass
class SessionAudit:
    session_id: str
    rollout_path: str
    cwd: str = ""
    title: str = ""
    first_user_message: str = ""
    git_branch: str = ""
    git_sha: str = ""
    model: str = ""
    created_at: str = ""
    updated_at: str = ""
    usage: Usage = field(default_factory=Usage)
    token_event_count: int = 0
    category: str = "unclassified"
    include_primary: bool = False
    include_with_plugin: bool = False
    keyword_only: bool = False
    classification_reason: str = ""
    state_tokens_used: int = 0


def to_iso(value: Any) -> str:
    if value in (None, ""):
        return ""
    try:
        number = int(value)
    except (TypeError, ValueError):
        return str(value)
    if number > 10_000_000_000:
        seconds = number / 1000
    else:
        seconds = number
    return datetime.fromtimestamp(seconds, timezone.utc).isoformat()


def load_threads(state_db: Path) -> dict[str, dict[str, Any]]:
    if not state_db.exists():
        return {}
    con = sqlite3.connect(state_db)
    con.row_factory = sqlite3.Row
    try:
        rows = con.execute(
            """
            select id, rollout_path, created_at, updated_at, cwd, title,
                   tokens_used, git_sha, git_branch, first_user_message,
                   model, reasoning_effort
            from threads
            """
        ).fetchall()
    finally:
        con.close()
    return {row["id"]: dict(row) for row in rows}


def load_history(history_path: Path) -> dict[str, str]:
    texts: dict[str, list[str]] = defaultdict(list)
    if not history_path.exists():
        return {}
    with history_path.open("r", encoding="utf-8", errors="replace") as handle:
        for line in handle:
            try:
                item = json.loads(line)
            except json.JSONDecodeError:
                continue
            sid = item.get("session_id")
            text = item.get("text")
            if sid and text:
                texts[sid].append(str(text))
    return {sid: "\n".join(parts) for sid, parts in texts.items()}


def extract_usage(payload: dict[str, Any]) -> Usage | None:
    if payload.get("type") != "token_count":
        return None
    info = payload.get("info") or {}
    total = info.get("total_token_usage", {})
    if not total:
        return None
    return Usage(
        input_tokens=int(total.get("input_tokens") or 0),
        cached_input_tokens=int(total.get("cached_input_tokens") or 0),
        output_tokens=int(total.get("output_tokens") or 0),
        reasoning_output_tokens=int(total.get("reasoning_output_tokens") or 0),
        total_tokens=int(total.get("total_tokens") or 0),
    )


def parse_rollout(path: Path, state: dict[str, Any] | None) -> SessionAudit:
    sid = path.stem.split("-")[-1]
    audit = SessionAudit(session_id=sid, rollout_path=str(path))
    if state:
        audit.session_id = state.get("id") or audit.session_id
        audit.cwd = state.get("cwd") or ""
        audit.title = state.get("title") or ""
        audit.first_user_message = state.get("first_user_message") or ""
        audit.git_branch = state.get("git_branch") or ""
        audit.git_sha = state.get("git_sha") or ""
        audit.model = state.get("model") or ""
        audit.created_at = to_iso(state.get("created_at_ms") or state.get("created_at"))
        audit.updated_at = to_iso(state.get("updated_at_ms") or state.get("updated_at"))
        audit.state_tokens_used = int(state.get("tokens_used") or 0)

    max_usage: Usage | None = None
    with path.open("r", encoding="utf-8", errors="replace") as handle:
        for line in handle:
            try:
                item = json.loads(line)
            except json.JSONDecodeError:
                continue
            typ = item.get("type")
            payload = item.get("payload") or {}
            if typ == "session_meta":
                audit.session_id = payload.get("id") or audit.session_id
                audit.cwd = audit.cwd or payload.get("cwd") or ""
                audit.created_at = audit.created_at or payload.get("timestamp") or ""
                audit.model = audit.model or payload.get("model") or ""
            if typ == "event_msg":
                usage = extract_usage(payload)
                if usage:
                    audit.token_event_count += 1
                    if max_usage is None or usage.total_tokens >= max_usage.total_tokens:
                        max_usage = usage
    if max_usage:
        audit.usage = max_usage
    return audit


def classify(audit: SessionAudit, history_text: str) -> None:
    searchable = "\n".join(
        [audit.cwd, audit.title, audit.first_user_message, history_text, audit.rollout_path]
    )
    cwd = audit.cwd
    if PLUGIN_PATH_RE.search(cwd):
        audit.category = "plugin-cwd"
        audit.include_with_plugin = True
        audit.classification_reason = "cwd is under Sussudio-Plugin"
        return
    if CORE_PATH_RE.search(cwd):
        audit.category = "core-cwd"
        audit.include_primary = True
        audit.include_with_plugin = True
        audit.classification_reason = "cwd path matches Sussudio/ElgatoCapture project family"
        return
    if PROJECT_NAME_RE.search(searchable):
        audit.category = "keyword-only"
        audit.keyword_only = True
        audit.classification_reason = "title/history/rollout text mentions a project alias"
        return
    audit.category = "other"


def add_totals(sessions: list[SessionAudit]) -> Usage:
    total = Usage()
    for session in sessions:
        total.add(session.usage)
    return total


def dedupe_sessions(sessions: list[SessionAudit]) -> list[SessionAudit]:
    best: dict[str, SessionAudit] = {}
    for session in sessions:
        previous = best.get(session.session_id)
        if previous is None or session.usage.total_tokens > previous.usage.total_tokens:
            best[session.session_id] = session
    return list(best.values())


def fmt(n: int) -> str:
    return f"{n:,}"


def usage_row(label: str, sessions: list[SessionAudit]) -> dict[str, Any]:
    total = add_totals(sessions)
    return {
        "label": label,
        "sessions": len(sessions),
        "with_token_events": sum(1 for s in sessions if s.token_event_count),
        "without_token_events": sum(1 for s in sessions if not s.token_event_count),
        "input_tokens_reported": total.input_tokens,
        "cached_input_tokens": total.cached_input_tokens,
        "uncached_input_tokens": total.uncached_input_tokens,
        "output_tokens_reported_includes_reasoning": total.output_tokens,
        "reasoning_output_tokens": total.reasoning_output_tokens,
        "visible_output_tokens_output_minus_reasoning": total.visible_output_tokens,
        "total_tokens_reported": total.total_tokens,
    }


def write_csv(path: Path, rows: list[dict[str, Any]]) -> None:
    if not rows:
        path.write_text("", encoding="utf-8")
        return
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)


def group_rows(label: str, sessions: list[SessionAudit], key_name: str, key_func: Any) -> list[dict[str, Any]]:
    groups: dict[str, list[SessionAudit]] = defaultdict(list)
    for session in sessions:
        key = key_func(session) or "(blank)"
        groups[str(key)].append(session)
    rows = []
    for key, grouped in groups.items():
        row = usage_row(label, grouped)
        row[key_name] = key
        rows.append(row)
    return sorted(rows, key=lambda row: row["total_tokens_reported"], reverse=True)


def month_key(session: SessionAudit) -> str:
    value = session.created_at
    if not value:
        return "(blank)"
    return value[:7]


def date_range(sessions: list[SessionAudit]) -> dict[str, str]:
    values = sorted(s.created_at for s in sessions if s.created_at)
    return {"first": values[0] if values else "", "last": values[-1] if values else ""}


def coarse_gap(sessions: list[SessionAudit]) -> dict[str, int]:
    missing = [s for s in sessions if not s.token_event_count and s.state_tokens_used]
    return {
        "sessions_without_split_token_events_but_with_state_tokens": len(missing),
        "coarse_state_tokens_for_missing_split_events": sum(s.state_tokens_used for s in missing),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--codex-home", default=r"C:\Users\crest\.codex")
    parser.add_argument("--out-dir", default=r"C:\Users\crest\source\repos\Sussudio\docs\codex-token-audit")
    args = parser.parse_args()

    codex_home = Path(args.codex_home)
    sessions_root = codex_home / "sessions"
    state = load_threads(codex_home / "state_5.sqlite")
    history = load_history(codex_home / "history.jsonl")
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    audits: list[SessionAudit] = []
    for path in sessions_root.rglob("*.jsonl"):
        session_id_match = re.search(
            r"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})",
            path.name,
        )
        sid = session_id_match.group(1) if session_id_match else path.stem
        audit = parse_rollout(path, state.get(sid))
        classify(audit, history.get(audit.session_id, ""))
        audits.append(audit)

    primary_rollouts = [s for s in audits if s.include_primary]
    with_plugin_rollouts = [s for s in audits if s.include_with_plugin]
    keyword_only_rollouts = [s for s in audits if s.keyword_only]
    upper_bound_rollouts = with_plugin_rollouts + keyword_only_rollouts

    primary = dedupe_sessions(primary_rollouts)
    with_plugin = dedupe_sessions(with_plugin_rollouts)
    keyword_only = dedupe_sessions(keyword_only_rollouts)
    upper_bound = dedupe_sessions(upper_bound_rollouts)

    summary_rows = [
        usage_row("primary_core_cwd_only", primary),
        usage_row("core_plus_sussudio_plugin_cwd", with_plugin),
        usage_row("keyword_only_candidates_not_in_primary", keyword_only),
        usage_row("upper_bound_cwd_plus_keyword_candidates", upper_bound),
    ]

    rollout_file_summary_rows = [
        usage_row("primary_core_cwd_only_rollout_file_sum", primary_rollouts),
        usage_row("core_plus_sussudio_plugin_cwd_rollout_file_sum", with_plugin_rollouts),
        usage_row("keyword_only_candidates_rollout_file_sum", keyword_only_rollouts),
        usage_row("upper_bound_rollout_file_sum", upper_bound_rollouts),
    ]

    session_rows: list[dict[str, Any]] = []
    for s in sorted(audits, key=lambda item: (item.created_at, item.session_id)):
        if s.include_with_plugin or s.keyword_only:
            session_rows.append(
                {
                    "session_id": s.session_id,
                    "category": s.category,
                    "included_primary_core": s.include_primary,
                    "included_core_plus_plugin": s.include_with_plugin,
                    "keyword_only": s.keyword_only,
                    "token_event_count": s.token_event_count,
                    "input_tokens_reported": s.usage.input_tokens,
                    "cached_input_tokens": s.usage.cached_input_tokens,
                    "uncached_input_tokens": s.usage.uncached_input_tokens,
                    "output_tokens_reported_includes_reasoning": s.usage.output_tokens,
                    "reasoning_output_tokens": s.usage.reasoning_output_tokens,
                    "visible_output_tokens_output_minus_reasoning": s.usage.visible_output_tokens,
                    "total_tokens_reported": s.usage.total_tokens,
                    "state_tokens_used": s.state_tokens_used,
                    "model": s.model,
                    "git_branch": s.git_branch,
                    "git_sha": s.git_sha,
                    "created_at": s.created_at,
                    "updated_at": s.updated_at,
                    "cwd": s.cwd,
                    "title": s.title,
                    "first_user_message": s.first_user_message.replace("\r", " ").replace("\n", " ")[:500],
                    "classification_reason": s.classification_reason,
                    "rollout_path": s.rollout_path,
                }
            )

    category_rows = []
    for category, count in Counter(s.category for s in audits).most_common():
        category_sessions = [s for s in audits if s.category == category]
        row = usage_row(category, category_sessions)
        category_rows.append(row)

    primary_month_rows = group_rows("primary_core_cwd_only", primary, "month_utc", month_key)
    primary_cwd_rows = group_rows("primary_core_cwd_only", primary, "cwd", lambda s: s.cwd)
    primary_branch_rows = group_rows("primary_core_cwd_only", primary, "git_branch", lambda s: s.git_branch)
    primary_model_rows = group_rows("primary_core_cwd_only", primary, "model", lambda s: s.model)

    write_csv(out_dir / "summary.csv", summary_rows)
    write_csv(out_dir / "rollout_file_summary.csv", rollout_file_summary_rows)
    write_csv(out_dir / "sessions.csv", session_rows)
    write_csv(out_dir / "categories.csv", category_rows)
    write_csv(out_dir / "primary_by_month.csv", primary_month_rows)
    write_csv(out_dir / "primary_by_cwd.csv", primary_cwd_rows)
    write_csv(out_dir / "primary_by_branch.csv", primary_branch_rows)
    write_csv(out_dir / "primary_by_model.csv", primary_model_rows)

    summary_json = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "codex_home": str(codex_home),
        "sessions_root": str(sessions_root),
        "rollout_files_scanned": len(audits),
        "dedupe_policy": "Top-level totals are deduped by session_id, keeping the row with the largest cumulative total_token_usage. This avoids double-counting resumed/compacted rollout archive files for the same thread.",
        "classification": {
            "primary_core_cwd_only": "cwd is Sussudio or any ElgatoCapture* worktree, excluding Sussudio-Plugin",
            "core_plus_sussudio_plugin_cwd": "primary plus cwd under Sussudio-Plugin",
            "keyword_only_candidates_not_in_primary": "not cwd-classified, but title/history/rollout path mentions project aliases",
            "upper_bound_cwd_plus_keyword_candidates": "core plus plugin plus keyword-only candidates; useful as a high-side estimate",
        },
        "summary": summary_rows,
        "rollout_file_summary_not_for_headline_totals": rollout_file_summary_rows,
        "categories": category_rows,
        "date_ranges": {
            "primary_core_cwd_only": date_range(primary),
            "core_plus_sussudio_plugin_cwd": date_range(with_plugin),
            "upper_bound_cwd_plus_keyword_candidates": date_range(upper_bound),
        },
        "coarse_gap_estimates": {
            "primary_core_cwd_only": coarse_gap(primary),
            "core_plus_sussudio_plugin_cwd": coarse_gap(with_plugin),
            "upper_bound_cwd_plus_keyword_candidates": coarse_gap(upper_bound),
        },
    }
    (out_dir / "summary.json").write_text(json.dumps(summary_json, indent=2), encoding="utf-8")

    lines = [
        "# Codex Token Audit: Sussudio / ElgatoCapture",
        "",
        f"Generated: {summary_json['generated_at_utc']}",
        f"Rollout JSONL files scanned: {fmt(len(audits))}",
        f"Primary unique sessions: {fmt(len(primary))}",
        f"Primary date range: {summary_json['date_ranges']['primary_core_cwd_only']['first']} to {summary_json['date_ranges']['primary_core_cwd_only']['last']}",
        "",
        "Token source: the largest cumulative `event_msg.token_count.info.total_token_usage` per unique `session_id`.",
        "If the same thread appears in multiple rollout archive files after resumes or compaction, the headline totals keep only the largest cumulative row for that `session_id`.",
        "`output_tokens_reported_includes_reasoning` is the Codex/OpenAI reported output token total; `visible_output_tokens_output_minus_reasoning` subtracts reported reasoning tokens.",
        "",
        "## Totals",
        "",
        "| Scope | Sessions | With token events | Cached input | Uncached input | Reasoning output | Reported output | Visible output | Reported total |",
        "|---|---:|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for row in summary_rows:
        lines.append(
            "| {label} | {sessions} | {with_token_events} | {cached} | {uncached} | {reasoning} | {output} | {visible} | {total} |".format(
                label=row["label"],
                sessions=fmt(row["sessions"]),
                with_token_events=fmt(row["with_token_events"]),
                cached=fmt(row["cached_input_tokens"]),
                uncached=fmt(row["uncached_input_tokens"]),
                reasoning=fmt(row["reasoning_output_tokens"]),
                output=fmt(row["output_tokens_reported_includes_reasoning"]),
                visible=fmt(row["visible_output_tokens_output_minus_reasoning"]),
                total=fmt(row["total_tokens_reported"]),
            )
        )
    lines += [
        "",
        "## Interpretation",
        "",
        "- `primary_core_cwd_only` is the conservative app-project total: sessions whose working directory was `Sussudio` or an `ElgatoCapture*` worktree.",
        "- `core_plus_sussudio_plugin_cwd` adds the Sussudio Stream Deck plugin sessions.",
        "- `keyword_only_candidates_not_in_primary` are not included in the conservative total because they may be cross-repo audits or meta sessions that merely mention Sussudio/ElgatoCapture.",
        "- `upper_bound_cwd_plus_keyword_candidates` is the high-side estimate if every keyword-only candidate is treated as project-related.",
        "",
        "## Files",
        "",
        "- `summary.csv`: same top-level totals in CSV form.",
        "- `rollout_file_summary.csv`: non-headline diagnostic totals before deduping duplicate rollout files.",
        "- `categories.csv`: totals by classifier bucket.",
        "- `primary_by_month.csv`, `primary_by_cwd.csv`, `primary_by_branch.csv`, `primary_by_model.csv`: breakdowns for the conservative app-project total.",
        "- `sessions.csv`: per-session evidence, including cwd, title, branch, SHA, model metadata when available, and rollout path.",
        "- `summary.json`: machine-readable summary.",
        "",
        "## Coverage Notes",
        "",
        f"- Conservative primary sessions missing split token events: {fmt(summary_rows[0]['without_token_events'])}.",
        f"- Coarse `state_5.sqlite.tokens_used` available for those missing primary sessions: {fmt(summary_json['coarse_gap_estimates']['primary_core_cwd_only']['coarse_state_tokens_for_missing_split_events'])} tokens across {fmt(summary_json['coarse_gap_estimates']['primary_core_cwd_only']['sessions_without_split_token_events_but_with_state_tokens'])} sessions. These are not allocated into cached/uncached/reasoning buckets.",
        "",
    ]
    (out_dir / "README.md").write_text("\n".join(lines), encoding="utf-8")
    print(json.dumps(summary_json, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
