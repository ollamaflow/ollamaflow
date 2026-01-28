import { SdkConfiguration } from '../base/SdkConfiguration';
import { SeverityEnum } from '../enums/SeverityEnum';

export default class Logger {
  private config: SdkConfiguration;

  /**
   * @param {SdkConfiguration} config
   */
  constructor(config: SdkConfiguration) {
    this.config = config;
  }

  /**
   * @param {SeverityEnum} severity
   * @param {string} message
   */
  log = (severity: SeverityEnum, message: string) => {
    switch (severity) {
      case SeverityEnum.Warn:
        if (this.config.logLevel === SeverityEnum.Warn || !this.config.logLevel) {
          //eslint-disable-next-line no-console
          console.warn(message);
        }
        break;
      case SeverityEnum.Debug:
        if (this.config.logLevel === SeverityEnum.Debug || !this.config.logLevel) {
          //eslint-disable-next-line no-console
          console.debug(message);
        }
        break;
      case SeverityEnum.Error:
        if (this.config.logLevel === SeverityEnum.Error || !this.config.logLevel) {
          //eslint-disable-next-line no-console
          console.error(message);
        }
        break;
      case SeverityEnum.Info:
        if (this.config.logLevel === SeverityEnum.Info || !this.config.logLevel) {
          //eslint-disable-next-line no-console
          console.log(message);
        }
        break;
      default:
        //eslint-disable-next-line no-console
        console.log(message);
    }
  };
}
