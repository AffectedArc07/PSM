import React, { useEffect, useState } from 'react';
import { LoginOutlined } from '@ant-design/icons';
import { Button, Card, Col, Divider, Form, Input, message, Row, Space, Spin, Typography } from 'antd';
import { API } from '../core/api_manager';
const { Text } = Typography;

export const Login = () => {
  const [locked, setLocked] = useState(false);

  const attemptLogin = async values => {
    setLocked(true);
    await API.attempt_login(values);
    setLocked(false);
  };

  return (
    <Row align="middle" justify="center" style={{ height: '100%' }}>
      <Col span={8}>
        <Space
          direction="vertical"
          size="large"
          style={{ width: '100%' }}>
          <Card title="PSM Login">
            <Form
              name="login"
              layout="vertical"
              autoComplete="off"
              initialValues={{ remember: true }}
              onFinish={attemptLogin}>
              <Form.Item
                label="Username"
                name="username"
                rules={[{ required: true }]}>
                <Input />
              </Form.Item>
              <Form.Item
                label="Password"
                name="password"
                rules={[{ required: true }]}>
                <Input.Password />
              </Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={locked}
                icon={<LoginOutlined />}>
                Sign in
              </Button>
            </Form>
          </Card>
        </Space>
      </Col>
    </Row>
  );
};
