/*
  StringUtils is a utility class for string operations.
*/
class Utils {
  /**
   * Encodes a string to base64.
   * @param {string} str - The string to encode.
   * @return {string} The encoded string.
   */
  static encodeBase64(str: string) {
    if (typeof window === 'undefined') {
      // Node.js
      return Buffer.from(str, 'utf-8').toString('base64');
    } else {
      // Browser
      const utf8Bytes = new TextEncoder().encode(str);
      const binaryString = Array.from(utf8Bytes, (byte) => String.fromCharCode(byte)).join('');
      return btoa(binaryString);
    }
  }

  static createBasicAuthHeader(email: string, password: string) {
    return `Basic ${Utils.encodeBase64(`${email}:${password}`)}`;
  }

  static createBearerAuthHeader(bearerToken: string) {
    return `Bearer ${bearerToken}`;
  }
}

export default Utils;
