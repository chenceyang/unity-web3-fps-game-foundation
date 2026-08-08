# Notice

The `Game.Web3` gateway contract and DTO shape are compatible with the user-provided
`timothyshen/web3-fps-assets` Unity SDK. As of 1.8.0 the `Runtime/Web3` and gateway/model
files under `Runtime/Tournaments` are verbatim mirrors of that SDK's `Runtime/` (including
the chain-config endpoint, reward state machine and single tournament intent route); the
remaining game foundation, match serialization, gameplay runtime and tests are supplied as
this project implementation.

No private keys, wallet credentials or RPC secrets are included.

The package includes two selected skeletal FBX character files from Quaternius' Ultimate
Animated Character Pack under CC0 1.0 Universal as replaceable reference assets. Generated
v1.9.1 scenes do not instantiate them. Their source, license and SHA-256 records are documented
in `Runtime/Formal/ThirdParty/Quaternius/README.md`.
