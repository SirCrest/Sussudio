# Templates

## Option Scoring Matrix

Score each proposed option from 1-5 for:

1. Correctness
2. Reliability
3. Performance
4. Latency
5. Project Fit
6. Complexity (inverse: higher score means simpler/safer to maintain)

Use weighted total:

- Correctness: 30%
- Reliability: 20%
- Performance: 15%
- Latency: 15%
- Project Fit: 10%
- Complexity: 10%

## Consensus Summary Template

```md
### Option Consensus
- Selected: <Option A / Option B / Hybrid>
- Why: <2-4 concise reasons>
- Rejected alternatives:
  - <Option>: <reason>

### Risk Register
1. <risk> | severity=<low/med/high> | mitigation=<action>
2. ...

### Validation Plan
1. <build/check/test>
2. ...
```

## Review Swarm Findings Template

```md
### Review Findings
1. [high|med|low] <title> - <file:line>
   - Impact: <behavioral/perf/reliability effect>
   - Fix: <required change>

### Verdicts
- performance-review: <pass/conditional/fail>
- latency-review: <pass/conditional/fail>
- reliability-review: <pass/conditional/fail>
- plan-accuracy-review: <pass/conditional/fail>
- integration-review: <pass/conditional/fail>

### Convergence
- Status: <converged/not converged>
- Remaining blockers: <none or list>
```
