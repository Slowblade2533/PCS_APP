import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import Big from 'big.js';

// Global configurations for financial/tax rounding (Thai Revenue Department standards)
Big.RM = 2; // ROUND_HALF_UP (if 3rd decimal place >= 5, rounds up; otherwise rounds down)
Big.DP = 4; // Max decimal places for intermediate division results

bootstrapApplication(App, appConfig).catch((err) => console.error(err));

