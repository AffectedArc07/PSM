import React, {useState} from 'react';
import {Login} from './pages/Login';
import {API} from './core/api_manager';
import Backend from "./components/backend";
import { Main } from './pages/Main';

export const App = () => {
  const [, setForceRefresh] = useState(0);
  API.setReloadState(setForceRefresh);

  if (!API.valid_token()) {
    return (
      <Login />
    );
  }

  return <Main />;

};
