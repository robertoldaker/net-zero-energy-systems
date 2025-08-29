import { enableProdMode, getModuleFactory } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import appSettings from '../../appsettings.json';

import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

export function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}

export function getDataUrl() {

    //
    // Done this way instead of using appSettings so that it can handle the "lv-app-tmp.net-zero-energy-systems.org" which is used
    // to act act as a temporary url whilst migrating website to new server
    //
    let hostname = window.location.hostname
    let protocol = window.location.protocol
    let url = ''
    if ( hostname.includes('app')) {
        hostname = hostname.replace('app','data')
        url =  `${protocol}//${hostname}`
    } else if ( hostname === 'localhost') {
        url = `${protocol}//${hostname}:5095`
    }
    console.log('dataUrl',url)
    return url;
    /*if (environment.production) {
        return appSettings.Production.DataUrl
    } else if (environment.staging) {
        return appSettings.Staging.DataUrl
    } else {
        return appSettings.Development.DataUrl
    }*/
}

export function getEvDemandUrl() {

    if (environment.production) {
        return appSettings.Production.EvDemandUrl
    } else if (environment.staging) {
        return appSettings.Staging.EvDemandUrl
    } else {
        return appSettings.Development.EvDemandUrl
    }
}

export function getMode() {
    if (environment.production) {
        return 'Production'
    } else if (environment.staging) {
        return 'Staging'
    } else {
        return 'Development'
    }
}

const providers = [
    { provide: 'BASE_URL', useFactory: getBaseUrl, deps: [] },
    { provide: 'DATA_URL', useFactory: getDataUrl, deps: [] },
    { provide: 'EV_DEMAND_URL', useFactory: getEvDemandUrl, deps: [] },
    { provide: 'MODE', useFactory: getMode, deps:[] }
];

if (environment.production || environment.staging) {
    enableProdMode();
}

platformBrowserDynamic(providers).bootstrapModule(AppModule)
    .catch(err => console.log(err));
