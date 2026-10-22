# KoshkiKode / Cordite Wars — Infrastructure

## Current state (as of Oct 2026)

- **Domain, DNS, registrar:** GoDaddy — `koshkikode.com` (registered through
  approx. Oct 2029). All A/CNAME records live in the GoDaddy DNS manager.
- **Release downloads:** self-hosted file server per
  [`docs/self-hosted-downloads.md`](../docs/self-hosted-downloads.md)
  (Docker + Caddy + Stripe Payment Links + GoDaddy DNS).
- **Planned managed platform:** Google Cloud / Firebase. KoshkiKode uses
  Google's ecosystem exclusively for managed hosting, storage, and databases
  going forward (no AWS — the account was deleted Oct 2026).
- **CI/CD:** GitHub Actions in `.github/workflows/`.

## Directories

- `aws-retired/` — the old AWS (S3 + CloudFront + Lambda + SES + DynamoDB)
  Terraform and Lambda sources. **Not deployed**; kept for reference. The
  licensing Lambda code (`aws-retired/lambda/`) is plain Python and remains
  the reference implementation for the license wire format — it can be
  re-hosted on Google Cloud (Cloud Run / Cloud Functions + Firestore +
  Secret Manager) when the time comes.
