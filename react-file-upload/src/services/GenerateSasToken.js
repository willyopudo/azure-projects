import { hmac } from "js-sha256";

export const generateSasToken = (resourceUri, keyName, key) => {
  const expiry = Math.floor(Date.now() / 1000) + 10800; // Valid for 3 hours
  const stringToSign = `${resourceUri}\n${expiry}`;

  // Generate HMAC hash (Uint8Array)
  const rawHmac = hmac.create(key).update(stringToSign).array();

  // Convert to Base64 manuallye
  const base64Hash = btoa(String.fromCharCode(...rawHmac));

  // Construct the token
  const token = `SharedAccessSignature sr=${encodeURIComponent(resourceUri)}&sig=${encodeURIComponent(base64Hash)}&se=${expiry}&skn=${keyName}`;

  return token;
};
