# Privacy Policy for VPOS Manager

**Last updated:** 2026-08-25

VPOS Manager ("the app") is a support tool published by DK.Software (David Kopf) for loading
VPOS PC dumps, managing VPOS sessions, and automating common dump-configuration edits.

## Summary

**VPOS Manager does not collect, transmit, or share any data with the developer or any third
party.** The app has no analytics, no telemetry, no advertising, and no network communication of
any kind beyond what you explicitly do yourself (e.g. the third-party VPOS Dump Editor tool you
point it at, which runs entirely on your own machine).

## What the app stores, and where

Everything the app stores stays **only on your own computer**, in your local user profile. Nothing
is ever uploaded, synced, or sent anywhere.

- **Settings** (registered VPOS versions, last-used dump path, and your toggle choices) are saved
  to a local `settings.json` file so they're remembered between sessions.
- **Credentials** (myVectron username/password, VectronConnect Connect ID/password) are only
  saved to that same local file if you explicitly turn on the optional "Save credentials"
  checkbox for that section — the app shows a warning before you do, since they're stored in
  plain text. If you leave those checkboxes off, credentials are kept in memory for the current
  session only and are discarded when the app closes.
- **Log files** (startup errors, key actions, and error details) are written to local, timestamped
  files for troubleshooting. These stay on your machine and are only useful if you choose to share
  them with someone helping you debug an issue.

None of the above ever leaves your computer through VPOS Manager itself.

## Third-party tools

VPOS Manager can optionally drive the third-party VPOS Dump Editor ("DIG") to edit a dump before
loading it. That tool runs locally on your machine under your control; VPOS Manager does not send
your dump files, credentials, or any other data to DIG's author or anyone else — it only invokes
the local executable you configure, the same way it would if you ran it yourself.

## Children's privacy

VPOS Manager is a professional support tool and is not directed at children. It does not knowingly
collect data from anyone, of any age, because it does not collect data at all.

## Changes to this policy

If this policy ever changes (for example, if a future version adds a feature that does
communicate over the network), this document will be updated to describe it accurately before
that version is released.

## Contact

Questions about this policy or the app can be directed to David Kopf (DK.Software).
