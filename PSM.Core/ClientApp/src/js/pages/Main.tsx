import React from 'react';
import {Layout, Menu} from 'antd';
import {UserOutlined, DatabaseOutlined} from '@ant-design/icons';
import Backend from "../components/backend";
import {UsersView} from "./UsersView";
import {InstanceView} from "./InstanceView";

const {Content, Sider} = Layout;

export type AppError = {
  error: string
  recoverable: boolean
  on_dismiss?: () => void
}

export class Main extends React.Component {
  componentDidMount() {
    Main._local = this
  }

  private app_errors: AppError[] = []
  private static _local: Main

  public static app_error(error: AppError) {
    const instance = Main._local
    instance.app_errors.push(error)
    instance.forceUpdate()
  }

  private render_errors(): JSX.Element {
    const recoverable = !this.app_errors.find(error => !error.recoverable)
    return (<>
      <h1 style={{"float": "none", "margin": "0 auto"}}>Warning: application errors have been caught</h1>
      <hr/>
      {this.app_errors.map(error => (<div key={error.error}>
        <p>{recoverable ? (
          <button onClick={() => {
            this.app_errors.splice(this.app_errors.indexOf(error), 1)
            if (error.on_dismiss) error.on_dismiss()
            this.forceUpdate()
          }}>Dismiss</button>) : (<button disabled={true}>Dismiss - Non-Recoverable Errors Exist</button>)}
          {" "}{error.error}</p>
      </div>))}
      {recoverable ? "" : (<button onClick={() => document.location.reload()}>Reload Application</button>)}
    </>)
  }

  render() {
    if (this.app_errors.length !== 0)
      return this.render_errors()

    // Get our tab
    const [navTab, setNavTab] = Backend.Global.useBackend(this, "navTab", "instances");

    const changePage = (e: any) => {
      setNavTab(e.key); // useBackend already registers this element for an update
    }

    const TargetTab = () => {
      switch (navTab) {
        case "instances": {
          return <InstanceView/>;
        }
        case "users": {
          return <UsersView/>;
        }
        default: {
          return (<h1>404</h1>);
        }
      }
    }

    return (
      <Layout style={{height: '100%'}}>
        <Content style={{height: '100%'}}>
          <Layout className="site-layout-background" style={{height: '100%'}}>
            <Sider
              breakpoint="lg"
              collapsedWidth="0"
              onBreakpoint={(broken) => {
                console.log(broken);
              }}
              onCollapse={(collapsed, type) => {
                console.log(collapsed, type);
              }}>
              <h2 style={{textAlign: "center", marginTop: "10px"}}>PSM</h2>
              <Menu
                mode="inline"
                defaultSelectedKeys={['instances']}
                onClick={changePage}
              >
                <Menu.Item key="instances" icon={<DatabaseOutlined/>}>Instances</Menu.Item>
                <Menu.Item key="users" icon={<UserOutlined/>}>Users</Menu.Item>
              </Menu>
            </Sider>
            <Content style={{padding: '0 24px', minHeight: 280}}>
              <TargetTab/>
            </Content>
          </Layout>
        </Content>
      </Layout>
    )
  }
}
