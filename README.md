# ReconSage CLI

**ReconSage** is a behavioral reconnaissance scanner.
It not only discovers targets, but also **observes how the server behaves under different conditions**.

---

## Why a CLI version?

ReconSage CLI is built for:

* terminal-focused users
* Windows users who want to do security research without Linux
* researchers who prefer **direct control** over scanning behavior

This version focuses on **raw observation and data collection**, not automation magic.

---

## Usage

ReconSage CLI uses a **flag-based interface**.

### Example

```bash
ReconSage.exe --waf --target http://localhost:3000/ \
  --concurrency 100 \
  --timeout 10 \
  --json /path/to/report.json \
  --wordlist /path/to/wordlist.txt
```

---

## Flag Breakdown

* `--waf`
  Scan mode selector (WAF detection in this case)

* `--target`
  Target base URL

* `--concurrency`
  Number of concurrent requests (required for async scanners)

* `--timeout`
  Request timeout in seconds (required)

* `--json`
  Output path for the generated JSON report

* `--wordlist`
  Path to wordlist used for directory-style scanning

---

## Scan Modes

* **Directory Scan** → `--dir`
* **Warmup Scan** → `--warmup`
* **Rate Limit Scan** → `--rate-limit`
* **WAF Scan** → `--waf`

---

## Use Case of Each Mode

### 1. Directory Scan

Performs directory-style brute forcing using the provided wordlist.

Example requests:

* `http://target/admin`
* `http://target/.gitignore`

Useful for:

* hidden paths
* misconfigured routes
* access control checks

---

### 2. Warmup Scan

Sends **harmless requests** to observe baseline server behavior.

Used to understand:

* latency baselines
* timeout thresholds
* concurrency tolerance

---

### 3. Rate Limit Scan

Tests whether the server enforces rate limiting and how it reacts under load.

---

### 4. WAF Scan

Detects:

* presence of a WAF
* severity of protection
* behavioral changes under different request patterns

---

## Ethical Notice

⚠️ **Do not abuse the `--rate-limit` mode**

This mode intentionally has minimal safeguards so researchers can observe real server behavior.
Abusing it may:

* disrupt services
* violate program rules
* get you banned

Use responsibly.

---

## Final Note

> Never give up because of errors.
> If you want to build, you will figure it out.

ReconSage is a result of curiosity and persistence.
