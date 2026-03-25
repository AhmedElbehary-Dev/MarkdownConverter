# 🍎 Setting up macOS Codesigning & Notarization

To resolve the "Damaged App" error, your GitHub Actions need access to your Apple Developer credentials. Follow these steps:

## 1. Get your Certificates
### Option A: Using a Mac
1. Open **Keychain Access**.
2. Find your **Developer ID Application** certificate.
3. Right-click and **Export** it as a `.p12` file (e.g., `signing.p12`).
4. Choose a password.

### Option B: No Mac? (Using OpenSSL)
If you don't have a Mac, you can generate the CSR (Certificate Signing Request) and convert the downloaded certificate to `.p12` using OpenSSL on Windows/Linux:

1. **Generate a Private Key & CSR**:
   ```bash
   # Create a private key
   openssl genrsa -out private.key 2048
   # Create the CSR (fill in your info)
   openssl req -new -key private.key -out request.csr
   ```
2. **Upload to Apple**: Go to [developer.apple.com](https://developer.apple.com/account/resources/certificates/add), select **Developer ID Application**, and upload `request.csr`.
3. **Download & Convert**: Download the `.cer` file from Apple (e.g., `developerID_application.cer`) and convert it to `.p12`:
   ```bash
   # Convert .cer to .pem
   openssl x509 -in developerID_application.cer -inform DER -out developerID_application.pem -outform PEM
   # Package to .p12 (Choose a password you'll remember!)
   openssl pkcs12 -export -out signing.p12 -inkey private.key -in developerID_application.pem
   ```

## 2. Prepare the Base64 String
In your terminal, encode the `.p12` file to Base64 so it can be stored safely as a GitHub Secret:
```bash
# Windows (PowerShell)
[Convert]::ToBase64String([IO.File]::ReadAllBytes("signing.p12")) | Out-File -FilePath cert_base64.txt

# Linux/macOS
base64 -i signing.p12 > cert_base64.txt
```

## 3. Add GitHub Secrets
Navigate to your repository: **Settings > Secrets and variables > Actions** and add these five secrets:

| Secret Name | Description |
| :--- | :--- |
| `MACOS_CERTIFICATE` | The content of `cert_base64.txt`. |
| `MACOS_CERTIFICATE_PWD` | The password you set when creating the `.p12`. |
| `APPLE_ID` | Your Apple ID email (e.g., `dev@example.com`). |
| `APPLE_ID_PASSWORD` | An **App-Specific Password** (generate at [appleid.apple.com](https://appleid.apple.com)). |
| `APPLE_TEAM_ID` | Your 10-character Team ID (found in [developer.apple.com](https://developer.apple.com/account)). |

---

## 🚫 Alternative: Manual Bypass (If not using Apple Developer Program)
If you choose not to sign the app, you must tell your users to run this command in their Terminal after downloading:

```bash
# For a DMG
sudo xattr -rd com.apple.quarantine /path/to/MarkdownConverter.dmg

# For the installed App
sudo xattr -rd com.apple.quarantine /Applications/MarkdownConverter.app
```
