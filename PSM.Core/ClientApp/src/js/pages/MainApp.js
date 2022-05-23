import React from 'react';
import { Typography } from 'antd';
const { Text } = Typography;

export const MainApp = () => {

  return (
    <>
      <Text>You are now logged in</Text>
      <button
        color={"red"}
        onClick={()=>API.debug_verify_token()}>Try Verify</button>
    </>
  );
};
