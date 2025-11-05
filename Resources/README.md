# Secure Stamp Service (Windows .NET) — README

> A local, HSM-like signing authority implemented as a Windows Service (.NET).
> Generates and holds a master private key ("stamp") inside a secure process, never exposes the key, supports Shamir Secret Sharing backups, and provides a secure local API for two Windows client apps to request cryptographic "stamps" (signed approvals) for mint, freeze, burn, move, and health-check operations.

---

## Table of Contents

1. Overview
2. Goals & Requirements
3. High-level architecture
4. Components
5. Security model
6. API (IPC) design & examples
7. Shamir Secret Sharing — backup & recovery
8. Admin/Dev utilities
9. Development setup
10. Build & run
11. Install as Windows Service
12. Testing
13. Logging & monitoring
14. Deployment & operational notes
15. Roadmap / TODO
16. References

---

## 1. Overview

This repository implements a secure signing service that behaves like a hidden stamp locked in a drawer:

* A master signing key is generated once and stored inside the service's secure storage (never exported).
* Daily usage: client apps submit requests; the service returns cryptographic signatures (stamped approvals).
* Recovery: master key backups are created using **Shamir's Secret Sharing**; pieces are distributed to trusted custodians.
* If the service fails, a new instance can be provided the required number of shares to reconstruct the same key inside its secure storage.

This project is intentionally Windows-specific and implemented as a .NET Windows Service to maximize OS-level security integration and availability.

---

## 2. Goals & Requirements

**Functional**

* Generate a master signing key (ECDSA or Ed25519).
* Never show or export the private key.
* Provide signing operations for: `mint`, `freeze`, `burn`, `move` and arbitrary `stamp` requests.
* Provide a `test-stamp` endpoint for health checks.
* Return signatures (not raw keys); clients deliver signed approvals to downstream systems.
* Backup master key with Shamir Secret Sharing (configurable threshold and share count).
* Recover the master key by providing ≥ threshold shares to a replacement service instance.

**Non-functional**

* Runs as a Windows Service; starts at boot.
* Strong local-only access controls: only local clients allowed (Named Pipes or localhost TLS/gRPC).
* Audit logging of signed operations (no private key leakage).
* Automatic restart capability under Windows Service Control Manager.
* Minimal dependency footprint; production-grade cryptography libraries.

---

## 3. High-level architecture

```
+----------------------+                +---------------------+
|  Client App A (Win)  | <--- IPC --->  |  Secure Stamp       |
+----------------------+                |  Windows Service    |
+----------------------+                |  (Signing Engine,   |
|  Client App B (Win)  | <--- IPC --->  |   Key Store, SSS)   |
+----------------------+                +---------------------+
                                             |
                                Admin tools (CLI / GUI) -- optional
                                             |
                                Share holders (out-of-band storage)
```

* IPC options: **Named Pipes** (recommended) or **gRPC over localhost with TLS**.
* Key storage: Windows-protected file (DPAPI), or store inside TPM/CNG/HSM if available.
* Backup: Shamir Secret Sharing to create offline shares.

---

## 4. Components

* `StampService` (Windows Service): main service process; manages key, handles requests, produces signatures.
* `KeyManager`: wraps key generation, secure storage, and in-memory key usage for signing. Ensures key never leaves protected memory.
* `SSSManager`: create shares, verify shares, reconstruct master key when threshold met.
* `IPCServer`: implements Named Pipe or gRPC server for client communication, authenticates clients, enforces ACLs.
* `AdminCLI` / `AdminGUI` (optional): for health checks, manual recovery workflow, generate test-stamps, show logs.
* `ClientLibrary` (NuGet/local DLL): a small library the two Windows apps use to talk to the service.
* `AuditLogger`: append-only audit log for signed operations (signature metadata, timestamp, caller id, request hash — never private key).
* `Tests`: unit and integration tests (including signing verification).

---

## 5. Security model

* **Private key stays internal**: The master private key is generated and used only inside the StampService process. The service API returns signatures only.
* **Secure storage**:

  * Primary: Windows Protected Data/DPAPI (CryptProtectData) or CNG/Ncrypt with a key stored in a protected key container.
  * If available, prefer a TPM-backed key provider or attach to a local HSM device.
* **Access control**:

  * IPC endpoint bound to local-only access.
  * Named Pipes security descriptor: restrict to specific users/groups or service accounts.
  * If gRPC/HTTP over localhost is used, require mTLS or local machine certificate checks.
* **Auditing**:

  * All sign requests logged with non-sensitive metadata.
* **Shamir shares**:

  * Each share is output as a compact file (or printed QR) and must be handled offline.
  * Each share holder must be able to verify their share without reconstructing the key (see next section).
* **Admin operations**:

  * Recovery requires a threshold of shares and offline presence of custodians (no automated cloud backup).
  * Any GUI/CLI admin must require strong authentication (Windows auth + role check).
* **Testing**:

  * `test-stamp` signs a benign message — signature verified publicly to confirm key live without revealing key.

---

## 6. API (IPC) design & examples

**Choice:** Named Pipes with a simple message protocol (protobuf or JSON-over-pipes). Alternatively, gRPC over local TLS.

### Methods

* `Sign(request)` → `SignedResponse`
* `Verify(signed)` → `Valid/Invalid` (optionally local-only)
* `TestStamp()` → `SignedTestResponse`
* `GetStatus()` → `ServiceStatus` (uptime, key present, last health time)
* `CreateShares(options)` → `ShareBundle` (admin only)
* `VerifyShare(share)` → `ShareValid` (admin / share-holder)
* `RecoverStart()` / `RecoverProvideShare(share)` → `RecoverStatus` (admin-only workflow)
* `Shutdown()` / `Restart()` (admin-only)

### Example `Sign` request (JSON-like)

```json
{
  "operation": "mint",
  "payload": {
    "recipient": "user123",
    "amount": 1000,
    "currency": "TOKENX",
    "nonce": "2025-11-04T12:00:00Z:random"
  },
  "requester_id": "ClientAppA",
  "auth": {
    "client_token": "..." // client library handles auth; may be optional if Named Pipe ACLs suffice
  }
}
```

### Example `SignedResponse`

```json
{
  "signature": "BASE64_SIGNATURE",
  "algorithm": "Ed25519",
  "signed_payload": "BASE64(payload)",
  "signer_id": "StampService-v1",
  "timestamp": "2025-11-04T12:00:01Z"
}
```

**Clients** must verify signature against the public key (public key may be published to token system or embedded in client verification module).

---

## 7. Shamir Secret Sharing — backup & recovery

* Use a well-tested SSS library (C#) implementing threshold secret sharing.
* Workflow:

  1. **CreateShares** (admin-only): Service reconstructs a backup of the private key (in memory), generates N shares with threshold T (configurable: e.g., 5 shares, threshold 3). The original key is *not* exported — instead you might run `KeyManager.ExportWrappedPrivateKey()` protected by service admin policy, only in memory for SSS creation. **Minimize exposure** in this step and wipe memory after usage.
  2. **Distribute shares**: Export formatted share files (or secure printouts) given to custodians via out-of-band secure channels.
  3. **Verify share**: Each holder runs `VerifyShare(share)` (a client tool that contacts service) which confirms the share is consistent with the public key (or with a commitment) — without reconstructing the key. Implementation note: publish a commitment (e.g., hash of the private key, or use Feldman VSS commitments) so a share-holder can verify their share is part of the distribution.
  4. **Recovery**:

     * Bring up new StampService on replacement host.
     * Admin triggers `RecoverStart`.
     * Share holders provide shares to the new service (via secure channel or physically paste into AdminCLI); once threshold T is reached, the service reconstructs the master private key into its key store and seals it. The service should then regenerate public commitments and audit the event.
* **Important**: Ensure that generating shares or reconstructing the key is an auditable, multi-person event (dual control) — do not allow a single admin to both request and provide shares trivially.

---

## 8. Admin/Dev utilities

* `AdminCLI`:

  * `status`
  * `test-stamp`
  * `create-shares --n 5 --t 3`
  * `verify-share <share-file>`
  * `recover --add-share <share-file>`
  * `install-service` / `uninstall-service` (helpers)
* `AdminGUI` (optional): tray-based app for health/status and initiating admin flows. Must require elevated privileges.

---

## 9. Development setup

**Prerequisites**

* Windows 10/11 or Windows Server
* .NET 8+ SDK (or target preferred LTS)
* Visual Studio 2022/2023 or VS Code
* (Optional) Local TPM/HSM hardware if available for testing
* Recommended libraries:

  * Cryptography: `System.Security.Cryptography` (built-in) or `BouncyCastle` for alternate algorithms.
  * Shamir SSS: include/implement a vetted library (search for `SecretSharing` or implement threshold scheme carefully).
  * IPC: `System.IO.Pipes` (Named Pipes) or `Grpc.Net.Client` + `Grpc.AspNetCore` for gRPC.
  * Logging: `Serilog` with secure sinks (file + optional SIEM).
* Developer account with admin rights (for service install).

**Project layout**

```
/src
  /StampService          (Windows service project)
  /StampService.Core     (logic: KeyManager, SSSManager, IPCServer)
  /ClientLib             (NuGet-style client for apps)
  /AdminCLI              (console admin tool)
  /AdminGUI              (optional WPF/WinForms)
  /Tests                 (unit/integration tests)
README.md
```

---

## 10. Build & run (development)

**Run locally (console mode)**

* The service project should support a console mode (`--console`) for development. This simplifies debugging.
* From repo root:

```bash
dotnet build
cd src/StampService
dotnet run -- --console
```

**Run as service**

* Build release, then install as Windows Service (see section 11).

**Client usage (dev)**

* Add `ClientLib` as reference in client apps; call `Sign()` with payload; verify returned signature using the public key.

---

## 11. Install as Windows Service

**Publish**

```powershell
dotnet publish -c Release -r win-x64
```

**Install**

* Use `sc.exe` or PowerShell:

```powershell
New-Service -Name "StampService" -BinaryPathName "C:\path\to\StampService.exe --service" -StartupType Automatic
Start-Service StampService
```

* Or, provide an installer (MSI) that sets service account, ACLs, and firewall rules if needed.

**Service account**

* Recommended to run under a dedicated service account with minimal privileges.
* Configure Named Pipe security to allow only that account and the two client apps' accounts.

---

## 12. Testing

**Unit tests**

* Key generation, signature, verification.
* SSS share generation, verification, reconstruction (positive & negative cases).
* IPC authentication and access control.

**Integration tests**

* Client -> Service sign flows.
* `test-stamp` full verification round-trip (signature verified by a separate verifier).
* Recovery workflow with shares stored in test fixtures.

**Security tests**

* Attempt to request private key export (should fail).
* Verify that no memory dumps contain raw private key after operations (use secure memory wipes).

---

## 13. Logging & monitoring

* Use an append-only audit log for sign operations: `timestamp, operation, request_hash, requester_id, signature_id, public_key_id`.
* Do not log private key material or raw shares.
* Implement alerts for:

  * Repeated failed sign attempts.
  * Share-creation events.
  * Recovery events (must trigger high-severity alert and manual review).

---

## 14. Deployment & operational notes

* Custodians: determine secure storage and handling policy for shares (physical safe, encrypted offline storage).
* Periodic health-checks: run `test-stamp` daily and report to monitoring.
* Key rotation: rare, but define a process: if rotating, you must create new key + new shares and retire the old key securely.
* Backup of public key / commitments: store in repository for share verification.

---

## 15. Roadmap / TODO

* Implement TPM-backed CNG provider fallback.
* Implement Feldman VSS commitments so share-holders can verify shares without reconstructing.
* Implement GUI admin console with multi-person confirmation workflows.
* Implement client-side libraries for easier integration (NuGet package).
* Harden IPC (mTLS or message signing) for extra protection.
* Automatic test-stamp scheduler and reporting.

---

## 16. References & suggested libraries

* .NET `System.Security.Cryptography` (built-in)
* BouncyCastle (for additional algorithms)
* Named Pipes: `System.IO.Pipes`
* gRPC: `Grpc.Net.Client` / `Grpc.AspNetCore`
* Logging: `Serilog`
* Shamir SSS: look for vetted C# implementations or port a small, audited implementation. (Do not write your own crypto primitives; use well-reviewed libraries.)
* Windows DPAPI / CNG docs for secure storage

---

## Appendix — Example signature verification flow

1. Client builds request object and computes `payload_hash = SHA256(payload)`.
2. Client calls `Sign(payload)` on StampService via Named Pipe.
3. StampService signs the payload hash with private key (e.g., Ed25519) and returns `{ signature, algo, signer_id, timestamp }`.
4. Client or downstream system verifies signature with the stored public key:

   * `verify(public_key, signature, payload_hash)` → true/false.
5. If true, client submits stamped approval to token system.

---

## Final notes

* Security is paramount: treat the key and share materials as highest-sensitivity items.
* Ensure multi-person control for share creation and recovery to avoid single-person takeover.
* Keep the service minimal and thoroughly audited. Prefer built-in well-tested cryptographic libraries over custom implementations.

---
