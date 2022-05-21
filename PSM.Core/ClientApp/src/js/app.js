import React, { useState } from 'react';
import { Login } from './pages/Login';
import { MainApp } from './pages/MainApp';
import { API } from './core/api_manager';

export const App = () => {
  const [forceRefresh, setForceRefresh] = useState(0);

  API.setReloadState(setForceRefresh);

  if (!API.valid_token()) {
    return (
      <Login />
    );
  }

  return <MainApp />;
};
