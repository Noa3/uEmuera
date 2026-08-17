# Reference fixtures

Fixtures are deterministic, tiny game inputs executed by the developer-provided
reference EM+EE binary and by uEmuera. A fixture is not verified because its
`.erb` path is listed in `REGRESSION_TESTS.json`.

The capture workflow is:

1. provide a real reference executable and a deterministic fixture command;
2. run `Reference/run_fixture.py capture --runtime reference`;
3. run the same fixture through uEmuera and capture `--runtime uemuera`;
4. inspect both JSON captures;
5. run `Reference/run_fixture.py verify`.

No expected output is generated from the uEmuera implementation alone.
