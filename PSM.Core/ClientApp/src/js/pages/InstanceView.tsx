import React from 'react';
import {Main} from "./Main";

export const InstanceView = () => {
  return (
    <div style={{ "margin": "16px" }}>
      <h1>Instances</h1>
      <button onClick={()=>Main.app_error({error: 'test error', recoverable: false})}>Test Non-Recoverable Error</button>
      <button onClick={()=>Main.app_error({error: 'test error', recoverable: true})}>Test Recoverable Error</button>
    </div>
  )
}
