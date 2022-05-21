import React from 'react';
import 'antd/dist/antd.dark.css';
import { createRoot } from 'react-dom/client';
import { App } from './app';

const root = createRoot(document.getElementById('root'));
root.render(<App />);
