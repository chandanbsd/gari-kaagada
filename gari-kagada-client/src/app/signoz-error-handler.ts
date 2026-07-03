import { ErrorHandler, Injectable } from '@angular/core';

import { logError } from './telemetry';

@Injectable()
export class SignozErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.error(error);
    logError(error);
  }
}
