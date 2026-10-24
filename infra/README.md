# KoshkiKode / Cordite Wars — Infrastructure

## Current state

- **Domain, DNS, registrar:** GoDaddy — `koshkikode.com` (registered through
  approx. Oct 2029). All A/CNAME records live in the GoDaddy DNS manager.
- **Release downloads:** self-hosted file server per
  [`docs/self-hosted-downloads.md`](../docs/self-hosted-downloads.md)
  (Docker + Caddy + Stripe Payment Links + GoDaddy DNS).
- **Managed platform direction:** Google Cloud / Firebase. Use Cloud Run or
  Firebase Hosting for services, Cloud Storage for release artifacts,
  Firestore for licensing state, and Secret Manager for signing credentials.
- **CI/CD:** GitHub Actions in `.github/workflows/`.

## Provider boundaries

GoDaddy is the registrar and DNS provider. Google Cloud is the only managed
cloud provider for future hosting, storage, databases, and secrets. GitHub
Actions remains the build and release automation system; it does not require
cloud-provider OIDC credentials for the current release flow.

The game itself is offline-first and does not require a cloud backend at
runtime. Licensing and paid downloads are optional distribution services.

The `licensing-reference/` directory contains only the shared license
wire-format implementation. It is not a cloud deployment. A hosted licensing
service is not currently included; any future service should use Google Cloud
services described above.
