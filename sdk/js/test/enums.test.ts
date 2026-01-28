import { ApiErrorEnum } from '../src/enums/ApiErrorEnum';
import { EnumerationOrderEnum } from '../src/enums/EnumerationOrderEnum';
import { SeverityEnum } from '../src/enums/SeverityEnum';

describe('Enums', () => {
  describe('ApiErrorEnum', () => {
    it('should be a frozen object', () => {
      expect(Object.isFrozen(ApiErrorEnum)).toBe(true);
    });

    it('should contain all expected error codes', () => {
      const expectedErrors = [
        'NoObjectMetadata',
        'NoObjectData',
        'NoMetadataRule',
        'RequiredPropertiesMissing',
        'NoGraphConnectivity',
        'GraphOperationFailed',
        'NoTypeDetectorConnectivity',
        'UnknownTypeDetected',
        'NoUdrConnectivity',
        'UdrGenerationFailed',
        'NoSemanticCellConnectivity',
        'SemanticCellExtractionFailed',
        'NoDataCatalogConnectivity',
        'DataCatalogPersistFailed',
        'UnknownDataCatalogType',
        'UnknownEmbeddingsGeneratorType',
        'EmbeddingsPersistFailed',
        'EmbeddingsGenerationFailed',
        'AuthenticationFailed',
        'AuthorizationFailed',
        'BadRequest',
        'Conflict',
        'DeserializationError',
        'Inactive',
        'InternalError',
        'InvalidRange',
        'InUse',
        'NotEmpty',
        'NotFound',
        'TooLarge',
      ];

      expectedErrors.forEach((error) => {
        expect(ApiErrorEnum).toHaveProperty(error);
        expect(ApiErrorEnum[error as keyof typeof ApiErrorEnum]).toBe(error);
      });
    });

    it('should have correct values for NoObjectMetadata', () => {
      expect(ApiErrorEnum.NoObjectMetadata).toBe('NoObjectMetadata');
    });

    it('should have correct values for NoObjectData', () => {
      expect(ApiErrorEnum.NoObjectData).toBe('NoObjectData');
    });

    it('should have correct values for AuthenticationFailed', () => {
      expect(ApiErrorEnum.AuthenticationFailed).toBe('AuthenticationFailed');
    });

    it('should have correct values for AuthorizationFailed', () => {
      expect(ApiErrorEnum.AuthorizationFailed).toBe('AuthorizationFailed');
    });

    it('should have correct values for NotFound', () => {
      expect(ApiErrorEnum.NotFound).toBe('NotFound');
    });

    it('should have correct values for BadRequest', () => {
      expect(ApiErrorEnum.BadRequest).toBe('BadRequest');
    });

    it('should have correct values for InternalError', () => {
      expect(ApiErrorEnum.InternalError).toBe('InternalError');
    });

    it('should prevent modification of properties', () => {
      expect(() => {
        (ApiErrorEnum as any).NoObjectMetadata = 'Modified';
      }).toThrow();
    });

    it('should prevent addition of new properties', () => {
      expect(() => {
        (ApiErrorEnum as any).NewError = 'NewError';
      }).toThrow();
    });

    it('should have exactly 30 error codes', () => {
      const keys = Object.keys(ApiErrorEnum);
      expect(keys.length).toBe(30);
    });

    it('should have all string values', () => {
      Object.values(ApiErrorEnum).forEach((value) => {
        expect(typeof value).toBe('string');
      });
    });
  });

  describe('EnumerationOrderEnum', () => {
    it('should contain all expected order values', () => {
      expect(EnumerationOrderEnum.CreatedAscending).toBe('CreatedAscending');
      expect(EnumerationOrderEnum.CreatedDescending).toBe('CreatedDescending');
      expect(EnumerationOrderEnum.NameAscending).toBe('NameAscending');
      expect(EnumerationOrderEnum.NameDescending).toBe('NameDescending');
      expect(EnumerationOrderEnum.GuidAscending).toBe('GuidAscending');
      expect(EnumerationOrderEnum.GuidDescending).toBe('GuidDescending');
      expect(EnumerationOrderEnum.CostAscending).toBe('CostAscending');
      expect(EnumerationOrderEnum.CostDescending).toBe('CostDescending');
    });

    it('should have exactly 8 enum values', () => {
      const values = Object.values(EnumerationOrderEnum).filter(
        (value) => typeof value === 'string'
      );
      expect(values.length).toBe(8);
    });

    it('should have all string values', () => {
      Object.values(EnumerationOrderEnum)
        .filter((value) => typeof value === 'string')
        .forEach((value) => {
          expect(typeof value).toBe('string');
        });
    });

    it('should be usable in switch statements', () => {
      const testOrder = EnumerationOrderEnum.CreatedAscending;
      let result = '';

      switch (testOrder) {
        case EnumerationOrderEnum.CreatedAscending:
          result = 'ascending';
          break;
        case EnumerationOrderEnum.CreatedDescending:
          result = 'descending';
          break;
        default:
          result = 'unknown';
      }

      expect(result).toBe('ascending');
    });

    it('should allow comparison with string values', () => {
      expect(EnumerationOrderEnum.NameAscending === 'NameAscending').toBe(true);
      expect(EnumerationOrderEnum.NameDescending === 'NameDescending').toBe(true);
    });

    it('should have CreatedAscending value', () => {
      expect(EnumerationOrderEnum.CreatedAscending).toBe('CreatedAscending');
    });

    it('should have CreatedDescending value', () => {
      expect(EnumerationOrderEnum.CreatedDescending).toBe('CreatedDescending');
    });

    it('should have NameAscending value', () => {
      expect(EnumerationOrderEnum.NameAscending).toBe('NameAscending');
    });

    it('should have NameDescending value', () => {
      expect(EnumerationOrderEnum.NameDescending).toBe('NameDescending');
    });

    it('should have GuidAscending value', () => {
      expect(EnumerationOrderEnum.GuidAscending).toBe('GuidAscending');
    });

    it('should have GuidDescending value', () => {
      expect(EnumerationOrderEnum.GuidDescending).toBe('GuidDescending');
    });

    it('should have CostAscending value', () => {
      expect(EnumerationOrderEnum.CostAscending).toBe('CostAscending');
    });

    it('should have CostDescending value', () => {
      expect(EnumerationOrderEnum.CostDescending).toBe('CostDescending');
    });
  });

  describe('SeverityEnum', () => {
    it('should contain all expected severity levels with correct numeric values', () => {
      expect(SeverityEnum.Debug).toBe(0);
      expect(SeverityEnum.Info).toBe(1);
      expect(SeverityEnum.Warn).toBe(2);
      expect(SeverityEnum.Error).toBe(3);
      expect(SeverityEnum.Alert).toBe(4);
      expect(SeverityEnum.Critical).toBe(5);
      expect(SeverityEnum.Emergency).toBe(6);
    });

    it('should have exactly 7 enum values', () => {
      const numericValues = Object.values(SeverityEnum).filter(
        (value) => typeof value === 'number'
      );
      expect(numericValues.length).toBe(7);
    });

    it('should have all numeric values', () => {
      Object.values(SeverityEnum)
        .filter((value) => typeof value === 'number')
        .forEach((value) => {
          expect(typeof value).toBe('number');
        });
    });

    it('should have sequential numeric values starting from 0', () => {
      const values = Object.values(SeverityEnum)
        .filter((value) => typeof value === 'number')
        .sort((a, b) => (a as number) - (b as number)) as number[];

      values.forEach((value, index) => {
        expect(value).toBe(index);
      });
    });

    it('should be usable in switch statements', () => {
      const testSeverity = SeverityEnum.Warn;
      let result = '';

      switch (testSeverity) {
        case SeverityEnum.Debug:
          result = 'debug';
          break;
        case SeverityEnum.Info:
          result = 'info';
          break;
        case SeverityEnum.Warn:
          result = 'warn';
          break;
        case SeverityEnum.Error:
          result = 'error';
          break;
        default:
          result = 'unknown';
      }

      expect(result).toBe('warn');
    });

    it('should allow comparison with numeric values', () => {
      expect(SeverityEnum.Debug === 0).toBe(true);
      expect(SeverityEnum.Info === 1).toBe(true);
      expect(SeverityEnum.Warn === 2).toBe(true);
      expect(SeverityEnum.Error === 3).toBe(true);
    });

    it('should allow comparison operators for severity levels', () => {
      expect(SeverityEnum.Debug < SeverityEnum.Info).toBe(true);
      expect(SeverityEnum.Warn < SeverityEnum.Error).toBe(true);
      expect(SeverityEnum.Error > SeverityEnum.Debug).toBe(true);
      expect(SeverityEnum.Critical > SeverityEnum.Alert).toBe(true);
    });

    it('should have Debug value of 0', () => {
      expect(SeverityEnum.Debug).toBe(0);
    });

    it('should have Info value of 1', () => {
      expect(SeverityEnum.Info).toBe(1);
    });

    it('should have Warn value of 2', () => {
      expect(SeverityEnum.Warn).toBe(2);
    });

    it('should have Error value of 3', () => {
      expect(SeverityEnum.Error).toBe(3);
    });

    it('should have Alert value of 4', () => {
      expect(SeverityEnum.Alert).toBe(4);
    });

    it('should have Critical value of 5', () => {
      expect(SeverityEnum.Critical).toBe(5);
    });

    it('should have Emergency value of 6', () => {
      expect(SeverityEnum.Emergency).toBe(6);
    });

    it('should allow reverse lookup by numeric value', () => {
      expect(SeverityEnum[0]).toBe('Debug');
      expect(SeverityEnum[1]).toBe('Info');
      expect(SeverityEnum[2]).toBe('Warn');
      expect(SeverityEnum[3]).toBe('Error');
    });
  });
});

