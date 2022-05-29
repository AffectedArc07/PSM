import React from 'react';
import 'antd/dist/antd.dark.css';
import {createRoot} from 'react-dom/client';
import {App} from './app';
import NavigationBar from "./components/navigation_bar";

const root = createRoot(document.getElementById('root'));
root.render(
  <>
    <NavigationBar/>
    <App/>
  </>
);
