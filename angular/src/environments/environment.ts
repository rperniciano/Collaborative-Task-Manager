import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44396/',
  redirectUri: baseUrl,
  clientId: 'CollaborativeTaskManager_App',
  responseType: 'code',
  scope: 'offline_access CollaborativeTaskManager',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'CollaborativeTaskManager',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44396',
      rootNamespace: 'CollaborativeTaskManager',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
