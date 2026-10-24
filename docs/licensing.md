# Licensing protocol

Cordite Wars contains a client-side license-key and entitlement protocol. The
game is offline-first, and the checked-in public-key placeholder disables the
licensing gate for source builds. **No hosted activation, renewal, purchase,
or license-issuance service is currently implemented or deployed.** Do not
enable the gate for a release until a compatible service and its key lifecycle
have been established.

## Implemented components

- `infra/licensing-reference/license_keys.py` — shared Python wire-format
  implementation and Ed25519 signing helpers.
- `src/Core/Licensing/` — game-side key/entitlement handling and an HTTP client
  for a future compatible service.
- `tools/license_keygen.py` — local keypair generation and test-key issuance.
- `tests/python/test_license_keys.py` — protocol format and cryptographic
  round-trip tests.

The retired cloud route handlers and their provider-specific integration
tests have been removed. Future hosted services must use Google Cloud
services; GoDaddy remains the registrar and DNS provider.

## License key

A key is 15 bytes encoded as 24 Crockford Base32 characters plus a CRC-8
check character. It is displayed as five groups of five:

```text
XXXXX-XXXXX-XXXXX-XXXXX-XXXXX
```

| Offset | Size | Field |
| --- | ---: | --- |
| 0 | 1 byte | Format version |
| 1 | 4 bytes | Random license identifier |
| 5 | 1 byte | Product SKU |
| 6 | 2 bytes | Issue date, days since 2025-01-01 |
| 8 | 1 byte | Reserved flags |
| 9 | 6 bytes | Truncated Ed25519 signature |

The short signature is not independently verifiable by the client. It is a
candidate-key filter for a future activation service. The full Ed25519-signed
entitlement is the client-side trust anchor.

## Entitlement

The signed entitlement is stored locally as a base64-url encoded binary blob:

| Offset | Size | Field |
| --- | ---: | --- |
| 0 | 1 byte | Format version |
| 1 | 4 bytes | License identifier |
| 5 | 16 bytes | Machine identifier |
| 21 | 1 byte | Machine slot |
| 22 | 4 bytes | Issue time, Unix seconds |
| 26 | 4 bytes | Expiry time, Unix seconds |
| 30 | 1 byte | Hostname byte length |
| 31 | Variable | Hostname hint, up to 64 UTF-8 bytes |
| End - 64 | 64 bytes | Ed25519 signature over the preceding bytes |

The client verifies the full signature against the public key embedded in the
game. The protocol currently supports a ten-machine slot limit and
approximately 400-day entitlements; an online service must enforce activation
and renewal policy before those behaviors are available to players.

## Storefront behavior

`StorefrontDetector.Detect(installDir)` recognizes `steam_appid.txt` and
`goggame-*.info`. Steam and GOG builds bypass the licensing gate; non-store
builds keep it disabled while the signing key is the all-zero placeholder.

## Local key handling

Generate a development signing keypair with:

```bash
python3 tools/license_keygen.py generate-signing-key
```

Keep the private key outside the repository and never commit it. Production
key storage should use Google Cloud Secret Manager if and when a hosted
licensing service is deployed. A real production signing key must not be
rotated casually: all entitlements signed by it would stop verifying unless
the client supports both the old and replacement public keys during a
controlled migration.

To inspect or mint local test keys, see `python3 tools/license_keygen.py
--help`. Do not use local key generation as a substitute for a production
issuance service.
