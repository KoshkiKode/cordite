# Cordite Wars — Downloads Web Page

> **Status:** This is a non-production front-end prototype for a possible
> paid-download flow. The checkout/download API and production host are not
> implemented.

A single static HTML page (`index.html`, no build step, no dependencies) that
demonstrates a proposed Stripe purchase flow. It does not currently gate
downloads or process payments.

## How it gets served

The release page is served by the configured downloads host. A request to
`https://downloads.koshkikode.com/` returns this page directly.

The file server and purchase API are not implemented by this reference page.
Release artifacts are published through GitHub Releases; future managed
hosting belongs on Google Cloud with GoDaddy DNS.

## Proposed flow (not implemented)

1. Visitor lands on `/`, page calls `fetch("releases/latest.json")` to render
   the per-platform list of artifacts (filenames + sizes only — no URLs).
2. Visitor clicks **Buy & download** → page POSTs `/api/checkout` → licensing service
   creates a Stripe Checkout Session → browser redirects to Stripe.
3. Stripe processes payment, fires `checkout.session.completed` webhook to
   `/api/webhook`. The licensing service verifies the signature and writes the
   order to its persistent store.
4. Stripe redirects the buyer back to `/?session_id=cs_…`. The page strips
   that parameter from the URL and stores the session ID in `localStorage`.
5. A future purchase API would authorize subsequent downloads and return a
   short-lived URL. No such API is currently deployed.

The intended flow would use a checkout-session ID to authorize downloads.
There is no deployed service to issue or validate that credential.

## Updating the page

Edit `index.html` directly and push. The next release publish (or a manual
deploy of the download host) re-uploads it and invalidates the cached
`/index.html`.

To preview locally:

```bash
cd web/downloads
python3 -m http.server 8000
# open http://localhost:8000
```

It will show the "no release published yet" empty state because there's no
`releases/latest.json` next to it, and the Buy / Download buttons will fail
because there's no `/api/*` backend — that's expected for local preview.

The purchase and download actions are non-functional until a compatible
backend is implemented and deployed. Do not use this page as a production
checkout flow.
