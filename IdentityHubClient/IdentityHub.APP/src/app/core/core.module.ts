import { NgModule } from '@angular/core';

/**
 * Core singletons are registered in `app.config.ts` (`provideHttpClient`, guards as functions, etc.).
 * This module exists to align with the enterprise folder layout and can wrap `importProvidersFrom` later if needed.
 */
@NgModule({})
export class CoreModule {}
