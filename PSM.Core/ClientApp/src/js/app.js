import React, {useState} from 'react';
import {Login} from './pages/Login';
import {API} from './core/api_manager';
import Backend from "./components/backend";

Backend.Global.setBackend("navTab", 0);
Backend.Global.setBackend("navTabs", [
  "Login", "Users"
]);

export const App = () => {
  const [, setForceRefresh] = useState(0);
  const [navTab] = Backend.Global.useBackend(this, "navTab", 0);
  API.setReloadState(setForceRefresh);

  switch (navTab) {
    case 0:
    default:
      return <Login/>
    case 1:
      return <Users/>
  }
};
