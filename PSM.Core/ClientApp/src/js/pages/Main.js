import React from 'react';
import { Layout, Menu, Breadcrumb } from 'antd';
import { UserOutlined, DatabaseOutlined } from '@ant-design/icons';
import { NavLink } from 'react-router-dom';
import Backend from "../components/backend";
import { UsersView } from "./UsersView";
import { InstanceView } from "./InstanceView";
const { Header, Content, Footer, Sider } = Layout;

export const Main = () => {
  // Get our tab
  const [, updateState] = React.useState();
  const [navTab, setNavTab] = Backend.Global.useBackend(this, "navTab", "instances");

  const changePage = e => {
    setNavTab(e.key);
    updateState({});
  }

  const TargetTab = () => {
    console.log(navTab);
    switch (navTab) {
      case "instances": {
        return <InstanceView />;
      }
      case "users": {
        return <UsersView />;
      }
      default: {
        return (<h1>404</h1>);
      }
    }
    console.log(navTab);
    return (<h1>test</h1>)
  }

  return(
    <Layout style={{ height: '100%' }}>
      <Content style={{ height: '100%' }}>
        <Layout className="site-layout-background" style={{ height: '100%' }}>
          <Sider
            breakpoint="lg"
            collapsedWidth="0"
            onBreakpoint={(broken) => {
              console.log(broken);
            }}
            onCollapse={(collapsed, type) => {
              console.log(collapsed, type);
            }}>
            <h2 style={{ textAlign: "center", marginTop: "10px" }}>PSM</h2>
            <Menu
              mode="inline"
              defaultSelectedKeys={['instances']}
              onClick={changePage}
            >
              <Menu.Item key="instances" icon={<DatabaseOutlined />}>Instances</Menu.Item>
              <Menu.Item key="users" icon={<UserOutlined />}>Users</Menu.Item>
            </Menu>
          </Sider>
          <Content style={{ padding: '0 24px', minHeight: 280 }}>
            <TargetTab />
          </Content>
        </Layout>
      </Content>
    </Layout >
  )
}
