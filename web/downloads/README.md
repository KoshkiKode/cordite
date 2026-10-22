# Cordite Wars — Downloads Web Page

> **Status (Oct 2026):** The AWS-hosted version of this flow (S3 + CloudFront +
> Lambda + DynamoDB, described below) is **retired** — the AWS account was
> deleted. These pages remain the reference front-end for the paywalled
> download flow and will be pointed at the self-hosted file server
> ([`docs/self-hosted-downloads.md`](../../docs/self-hosted-downloads.md))
> or a Google Cloud re-host. Treat the AWS specifics below as historical.

A single static HTML page (`index.html`, no build step, no dependencies) that
fronts the AWS release-hosting bucket and **gates downloads behind a Stripe
one-time purchase**. No accounts, no admin panel, no user picker.

## How it gets served

This page is uploaded to `s3://<bucket>/public/index.html` by
`.github/workflows/deploy-aws.yml` on every release. CloudFront's
`default_root_object = "index.html"` setting means a request to the CDN apex
(`https://downloads.koshkikode.com/`) returns this page directly.

```
s3://<bucket>/
├── public/                         ← reachable via CloudFront
│   ├── index.html                      (this file)
│   └── releases/
│       └── latest.json                 (version + file manifest)
└── paid/                           ← NOT served via CloudFront
    └── <version>/
        ├── CorditeWars-Setup.exe       (only handed out as
        ├── CorditeWars.dmg              short-TTL presigned URLs
        └── …                            by the paywall Lambda)
```

## Paywall flow

1. Visitor lands on `/`, page calls `fetch("releases/latest.json")` to render
   the per-platform list of artifacts (filenames + sizes only — no URLs).
2. Visitor clicks **Buy & download** → page POSTs `/api/checkout` → Lambda
   creates a Stripe Checkout Session → browser redirects to Stripe.
3. Stripe processes payment, fires `checkout.session.completed` webhook to
   `/api/webhook`. Lambda verifies the signature and writes the order to
   DynamoDB.
4. Stripe redirects the buyer back to `/?session_id=cs_…`. The page strips
   that parameter from the URL and stores the session ID in `localStorage`.
5. Subsequent clicks on a download link call `/api/download?session_id=…&
   filename=…`. Lambda atomically increments the redemption counter (subject
   to a configurable max), validates the filename against the current
   manifest, and 302s to a short-TTL S3 presigned URL.

The session ID is the only credential the buyer ever sees; reinstalling /
switching platforms just re-uses it.

## Updating the page

Edit `index.html` directly and push. The next release publish (or a manual
deploy of the download host (was: the retired **Deploy Release Artifacts to
AWS** workflow) re-uploads it and invalidates the cached `/index.html`.

To preview locally:

```bash
cd web/downloads
python3 -m http.server 8000
# open http://localhost:8000
```

It will show the "no release published yet" empty state because there's no
`releases/latest.json` next to it, and the Buy / Download buttons will fail
because there's no `/api/*` backend — that's expected for local preview.

For end-to-end testing, run the flow against the current download host
(self-hosted per `docs/self-hosted-downloads.md`, or the future Google Cloud
deployment) with Stripe **test-mode** keys.
