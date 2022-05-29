import React, {useState} from 'react';
import {Login} from './pages/Login';
import {API} from './core/api_manager';
import {Main} from './pages/Main';

export const App = () => {
  const [sfState, setForceRefresh] = useState(0);
  API.setReloadState(() => setForceRefresh(sfState + 1));

  if (!API.valid_token()) {
    return (
      <Login/>
    );
  }

  return <Main/>;

};
