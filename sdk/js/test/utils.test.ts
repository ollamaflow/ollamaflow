import Stream from '../src/utils/Stream';
import Logger from '../src/utils/Logger';
import Utils from '../src/utils/Utils';
import Serializer from '../src/utils/Serializer';
import { SdkConfiguration } from '../src/base/SdkConfiguration';
import { SeverityEnum } from '../src/enums/SeverityEnum';

describe('Utils', () => {
  describe('Stream', () => {
    describe('createStreamParser', () => {
      it('should create a writable stream that calls onToken callback', (done) => {
        const tokens: string[] = [];
        const onToken = (token: string) => {
          tokens.push(token);
        };

        const stream = Stream.createStreamParser(onToken);

        // Write test data
        stream.write('line1\nline2\nline3', 'utf8', () => {
          stream.end(() => {
            expect(tokens).toEqual(['line1', 'line2', 'line3']);
            done();
          });
        });
      });

      it('should handle empty lines', (done) => {
        const tokens: string[] = [];
        const onToken = (token: string) => {
          if (token) {
            tokens.push(token);
          }
        };

        const stream = Stream.createStreamParser(onToken);

        stream.write('\n\nline1\n\n', 'utf8', () => {
          stream.end(() => {
            expect(tokens.length).toBeGreaterThan(0);
            expect(tokens).toContain('line1');
            done();
          });
        });
      });

      it('should handle multiple chunks', (done) => {
        const tokens: string[] = [];
        const onToken = (token: string) => {
          if (token) {
            tokens.push(token);
          }
        };

        const stream = Stream.createStreamParser(onToken);

        stream.write('chunk1\nchunk2', 'utf8', () => {
          stream.write('\nchunk3\n', 'utf8', () => {
            stream.end(() => {
              expect(tokens).toContain('chunk1');
              expect(tokens).toContain('chunk2');
              expect(tokens).toContain('chunk3');
              done();
            });
          });
        });
      });
    });

    describe('extractToken', () => {
      it('should extract token from valid JSON', () => {
        const json = JSON.stringify({ token: 'test-token-123' });
        const token = Stream.extractToken(json);
        expect(token).toBe('test-token-123');
      });

      it('should return null when token is missing', () => {
        const json = JSON.stringify({ data: 'some data' });
        const token = Stream.extractToken(json);
        expect(token).toBeNull();
      });

      it('should return null for invalid JSON', () => {
        const invalidJson = 'not a valid json {';
        const token = Stream.extractToken(invalidJson);
        expect(token).toBeNull();
      });

      it('should return null for empty string', () => {
        const token = Stream.extractToken('');
        expect(token).toBeNull();
      });

      it('should handle token with special characters', () => {
        const json = JSON.stringify({ token: 'token-with-special-chars-!@#$%' });
        const token = Stream.extractToken(json);
        expect(token).toBe('token-with-special-chars-!@#$%');
      });
    });
  });

  describe('Logger', () => {
    let consoleSpy: {
      debug: jest.SpyInstance;
      warn: jest.SpyInstance;
      error: jest.SpyInstance;
      log: jest.SpyInstance;
    };

    beforeEach(() => {
      consoleSpy = {
        debug: jest.spyOn(console, 'debug').mockImplementation(),
        warn: jest.spyOn(console, 'warn').mockImplementation(),
        error: jest.spyOn(console, 'error').mockImplementation(),
        log: jest.spyOn(console, 'log').mockImplementation(),
      };
    });

    afterEach(() => {
      jest.restoreAllMocks();
    });

    it('should log debug messages when logLevel is Debug', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      config.logLevel = SeverityEnum.Debug;
      const logger = new Logger(config);

      logger.log(SeverityEnum.Debug, 'Debug message');
      expect(consoleSpy.debug).toHaveBeenCalledWith('Debug message');
    });

    it('should log debug messages when logLevel is not set', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      const logger = new Logger(config);

      logger.log(SeverityEnum.Debug, 'Debug message');
      expect(consoleSpy.debug).toHaveBeenCalledWith('Debug message');
    });

    it('should not log debug messages when logLevel is higher than Debug', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      config.logLevel = SeverityEnum.Warn;
      const logger = new Logger(config);

      logger.log(SeverityEnum.Debug, 'Debug message');
      expect(consoleSpy.debug).not.toHaveBeenCalled();
    });

    it('should log warn messages when logLevel is Warn', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      config.logLevel = SeverityEnum.Warn;
      const logger = new Logger(config);

      logger.log(SeverityEnum.Warn, 'Warning message');
      expect(consoleSpy.warn).toHaveBeenCalledWith('Warning message');
    });

    it('should log warn messages when logLevel is not set', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      const logger = new Logger(config);

      logger.log(SeverityEnum.Warn, 'Warning message');
      expect(consoleSpy.warn).toHaveBeenCalledWith('Warning message');
    });

    it('should log error messages when logLevel is Error', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      config.logLevel = SeverityEnum.Error;
      const logger = new Logger(config);

      logger.log(SeverityEnum.Error, 'Error message');
      expect(consoleSpy.error).toHaveBeenCalledWith('Error message');
    });

    it('should log error messages when logLevel is not set', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      const logger = new Logger(config);

      logger.log(SeverityEnum.Error, 'Error message');
      expect(consoleSpy.error).toHaveBeenCalledWith('Error message');
    });

    it('should log info messages when logLevel is Info', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      config.logLevel = SeverityEnum.Info;
      const logger = new Logger(config);

      logger.log(SeverityEnum.Info, 'Info message');
      expect(consoleSpy.log).toHaveBeenCalledWith('Info message');
    });

    it('should log info messages when logLevel is not set', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      const logger = new Logger(config);

      logger.log(SeverityEnum.Info, 'Info message');
      expect(consoleSpy.log).toHaveBeenCalledWith('Info message');
    });

    it('should use console.log for unknown severity levels', () => {
      const config = new SdkConfiguration({ endpoint: 'http://test.com/' });
      const logger = new Logger(config);

      logger.log(999 as SeverityEnum, 'Unknown severity message');
      expect(consoleSpy.log).toHaveBeenCalledWith('Unknown severity message');
    });
  });

  describe('Utils', () => {
    describe('encodeBase64', () => {
      it('should encode a string to base64', () => {
        const input = 'Hello World';
        const encoded = Utils.encodeBase64(input);
        expect(encoded).toBe('SGVsbG8gV29ybGQ=');
      });

      it('should encode empty string', () => {
        const encoded = Utils.encodeBase64('');
        expect(encoded).toBe('');
      });

      it('should encode string with special characters', () => {
        const input = 'test@example.com:password123';
        const encoded = Utils.encodeBase64(input);
        expect(encoded).toBe('dGVzdEBleGFtcGxlLmNvbTpwYXNzd29yZDEyMw==');
      });

      it('should encode unicode characters', () => {
        const input = 'Hello 世界';
        const encoded = Utils.encodeBase64(input);
        // Verify it's valid base64
        expect(encoded).toMatch(/^[A-Za-z0-9+/=]+$/);
        expect(encoded.length).toBeGreaterThan(0);
      });
    });

    describe('createBasicAuthHeader', () => {
      it('should create basic auth header', () => {
        const header = Utils.createBasicAuthHeader('user@example.com', 'password123');
        expect(header).toBe('Basic dXNlckBleGFtcGxlLmNvbTpwYXNzd29yZDEyMw==');
      });

      it('should handle empty credentials', () => {
        const header = Utils.createBasicAuthHeader('', '');
        expect(header).toBe('Basic Og==');
      });

      it('should handle special characters in credentials', () => {
        const header = Utils.createBasicAuthHeader('user@domain.com', 'p@ssw0rd!');
        expect(header).toMatch(/^Basic /);
        expect(header.length).toBeGreaterThan(6);
      });
    });

    describe('createBearerAuthHeader', () => {
      it('should create bearer auth header', () => {
        const header = Utils.createBearerAuthHeader('token123');
        expect(header).toBe('Bearer token123');
      });

      it('should handle empty token', () => {
        const header = Utils.createBearerAuthHeader('');
        expect(header).toBe('Bearer ');
      });

      it('should handle token with special characters', () => {
        const token = 'token-with-special-chars-!@#$%';
        const header = Utils.createBearerAuthHeader(token);
        expect(header).toBe(`Bearer ${token}`);
      });
    });
  });

  describe('Serializer', () => {
    describe('deserializeJson', () => {
      it('should deserialize valid JSON string', () => {
        const jsonString = '{"name":"test","value":123}';
        const result = Serializer.deserializeJson(jsonString);
        expect(result).toEqual({ name: 'test', value: 123 });
      });

      it('should return the same object if input is already an object', () => {
        const obj = { name: 'test', value: 123 };
        const result = Serializer.deserializeJson(obj as any);
        expect(result).toEqual(obj);
      });

      it('should return the string if JSON parsing fails', () => {
        const invalidJson = 'not a valid json';
        const consoleSpy = jest.spyOn(console, 'log').mockImplementation();
        const result = Serializer.deserializeJson(invalidJson);
        expect(result).toBe(invalidJson);
        expect(consoleSpy).toHaveBeenCalled();
        consoleSpy.mockRestore();
      });

      it('should handle empty string', () => {
        const result = Serializer.deserializeJson('');
        expect(result).toBe('');
      });

      it('should handle null input', () => {
        const result = Serializer.deserializeJson(null as any);
        expect(result).toBeNull();
      });

      it('should handle array JSON', () => {
        const jsonString = '[1,2,3,{"key":"value"}]';
        const result = Serializer.deserializeJson(jsonString);
        expect(result).toEqual([1, 2, 3, { key: 'value' }]);
      });

      it('should handle nested objects', () => {
        const jsonString = '{"nested":{"deep":{"value":42}}}';
        const result = Serializer.deserializeJson(jsonString);
        expect(result).toEqual({ nested: { deep: { value: 42 } } });
      });
    });

    describe('serializeJson', () => {
      it('should serialize object to JSON with pretty print by default', () => {
        const obj = { name: 'test', value: 123 };
        const result = Serializer.serializeJson(obj);
        expect(result).toContain('\n');
        expect(result).toContain('"name"');
        expect(result).toContain('"test"');
      });

      it('should serialize object to JSON without pretty print when pretty is false', () => {
        const obj = { name: 'test', value: 123 };
        const result = Serializer.serializeJson(obj, false);
        expect(result).not.toContain('\n');
        expect(result).toBe('{"name":"test","value":123}');
      });

      it('should return null for null input', () => {
        const result = Serializer.serializeJson(null);
        expect(result).toBeNull();
      });

      it('should serialize array', () => {
        const arr = [1, 2, 3, { key: 'value' }];
        const result = Serializer.serializeJson(arr);
        expect(JSON.parse(result!)).toEqual(arr);
      });

      it('should serialize nested objects', () => {
        const obj = { nested: { deep: { value: 42 } } };
        const result = Serializer.serializeJson(obj);
        expect(JSON.parse(result!)).toEqual(obj);
      });
    });

    describe('jsonReplacer', () => {
      it('should convert Date objects to ISO strings', () => {
        const date = new Date('2024-01-01T00:00:00.000Z');
        const result = Serializer.jsonReplacer('date', date);
        expect(result).toBe('2024-01-01T00:00:00.000Z');
      });

      it('should return other values as-is', () => {
        expect(Serializer.jsonReplacer('string', 'test')).toBe('test');
        expect(Serializer.jsonReplacer('number', 123)).toBe(123);
        expect(Serializer.jsonReplacer('boolean', true)).toBe(true);
        expect(Serializer.jsonReplacer('null', null)).toBeNull();
      });

      it('should handle Date in serializeJson', () => {
        const obj = { date: new Date('2024-01-01T00:00:00.000Z'), name: 'test' };
        const result = Serializer.serializeJson(obj);
        const parsed = JSON.parse(result!);
        expect(parsed.date).toBe('2024-01-01T00:00:00.000Z');
        expect(parsed.name).toBe('test');
      });
    });
  });
});
