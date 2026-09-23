# Performance timeline compatibility fixtures

These expected 159-field timeline outputs were captured from the original layered builder at commit `cf9680b466b1419a87cf4c95dddb1a0911bbcc2c`, tree `5b199d341f06b1116c66ea4d2c82674bf4b69729`.

The validated app DLL SHA-256 was `CCE5763998D2C2E473EA8A3320566DDF03141EB01A526513BA0874E0B2F055E3`. Both captures were repeated and matched. Each fixture pairs the original snapshot inputs read by that builder with its complete captured output. Unread snapshot properties are omitted; the reduced inputs were separately checked against that same original DLL before the builder changed.

The snapshot inputs retain original raw strings and nulls independently of later health-model corrections. The tests invoke the actual timeline builder and compare every serialized field by name, kind and value. Tests never regenerate expected values.
