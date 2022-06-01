import React from 'react';
import {API} from "../core/api_manager";
import api_users, {UserInformation, UserPermissionModal} from "../core/api_users";
import NavigationBar, {NavigationButton} from "../components/navigation_bar";
// import NavigationBar from "../components/navigation_bar";

type UsersViewProps = {}

type UsersViewState = {
  user_list: UserInformation[]
  active_user: UserInformation
  active_page: number
  permission_modals: UserPermissionModal[]
}

const PageConstants = {
  DEFAULT_PAGE: 1,
  CREATE_USER: 2,
  EDIT_USER: 3,
}

export class UsersView extends React.Component<UsersViewProps, UsersViewState> {
  protected user_api: api_users = new api_users(API);

  componentDidMount() {
    if (!this.state?.active_page)
      this.setState({active_page: PageConstants.DEFAULT_PAGE})
    this.start_api_update();
  }

  public start_api_update() {
    this.user_api.get_users().then(() => this.updateUserList(this.user_api.user_list));
    this.user_api.get_permission_modals().then(() => this.updatePermissionModals(this.user_api.permission_modals))
  }

  private updateUserList(users: UserInformation[]) {
    const setState = (): void => {
      console.log("setting user state")
      this.setState({user_list: users})
    }
    console.log("checking user list")
    if (users.length === 0)
      return
    if (!this.state?.user_list)
      return setState();
    if (users.length !== this.state.user_list.length)
      return setState()
    const newUsers = users.map(user => user.username);
    const oldUsers = this.state.user_list.map(user => user.username);
    newUsers.forEach(newUser => {
      if (!oldUsers.includes(newUser))
        return setState()
      oldUsers.splice(oldUsers.indexOf(newUser), 1);
    })
    oldUsers.forEach(oldUser => {
      if (!newUsers.includes(oldUser))
        return setState()
    })
    console.log("didnt need to refresh user list")
  }

  private updatePermissionModals(modals: UserPermissionModal[]) {
    const setState = (): void => {
      console.log("setting user state")
      this.setState({permission_modals: modals})
    }
    console.log("checking user list")
    if (modals.length === 0)
      return
    if (!this.state?.permission_modals)
      return setState();
    if (modals.length !== this.state.permission_modals.length)
      return setState()
    const newModals = modals.map(modal => modal.name);
    const oldModals = this.state.permission_modals.map(modal => modal.name);
    newModals.forEach(newModal => {
      if (!oldModals.includes(newModal))
        return setState()
      oldModals.splice(oldModals.indexOf(newModal), 1);
    })
    oldModals.forEach(oldModal => {
      if (!newModals.includes(oldModal))
        return setState()
    })
    console.log("didnt need to refresh permission modals")
  }

  private nav_from_user(user: UserInformation | undefined, buttons: NavigationButton[]) {
    const found = buttons.find(button => button.name === user?.username)
    console.log(`nav button from user found: ${found?.name}`)
    return found
  }

  private nav_buttons(): JSX.Element[] {
    const cancel = <button key='cancel_button'
                           onClick={() => this.setState({active_page: PageConstants.DEFAULT_PAGE})}>Cancel</button>
    const user_actions = <>
      <button
        key='user_button_edit'
        onClick={() => this.setState({active_page: PageConstants.EDIT_USER})}>
        Edit User
      </button>
      <button
        key='user_button_create'
        onClick={() => this.setState({active_page: PageConstants.CREATE_USER})}>
        Create User
      </button>
    </>
    let ret: JSX.Element[] = [];
    if (this.state?.active_page !== PageConstants.DEFAULT_PAGE)
      ret.push(cancel);
    else {
      if (this.state?.active_user)
        ret.push(user_actions)
    }
    return ret;
  }

  private page_title(): string | undefined {
    switch (this.state?.active_page) {
      default:
      case PageConstants.DEFAULT_PAGE: {
        return "List"
      }
      case PageConstants.CREATE_USER: {
        return "Creating"
      }
      case PageConstants.EDIT_USER: {
        return "Modifying"
      }
    }
  }

  private render_create_user(): JSX.Element {
    return (
      <p>todo</p>
    )
  }

  private render_edit_user(): JSX.Element {
    console.log(`${this.state.permission_modals.length} permission modals`)
    console.log(this.state.permission_modals)
    return (<div>
      <p>Modals: </p>
      {this.state.permission_modals.map(modal => (
        <p key={modal.name}>[{modal.id}] {modal.name}</p>
      ))}
    </div>)
  }

  private static render_default(): JSX.Element {
    return (
      <p>Please select a task!</p>
    )
  }

  private get_render_contents() {
    switch (this.state.active_page) {
      default:
      case PageConstants.DEFAULT_PAGE:
        return UsersView.render_default();
      case PageConstants.CREATE_USER:
        return this.render_create_user();
      case PageConstants.EDIT_USER:
        return this.render_edit_user();
    }
  }

  render() {
    if (!this.state?.user_list || this.state.user_list.length === 0) {
      return (
        <p>Loading</p>
      )
    }
    const nav_map = this.state.user_list.map(user => {
      return {
        name: user.username,
        callback: () => {
          this.setState({
            active_user: user
          })
        },
      }
    });

    const page_title = this.page_title();
    const title_user = this.state?.active_user ? this.state.active_user.username : `No User Selected`
    const actual_title = `Users - ${title_user} - ${page_title}`
    const page_contents = this.get_render_contents();

    return (
      <div style={{"margin": "16px"}}>
        <h1>
          {actual_title}
          <div id={'nav-buttons'} style={{"float": "right"}}>
            {this.nav_buttons()}
          </div>
        </h1>
        <hr/>
        <NavigationBar disabled={this.state.active_page !== PageConstants.DEFAULT_PAGE} buttons={nav_map}
                       selected_button={this.nav_from_user(this.state?.active_user, nav_map)}/>
        <hr/>
        <div>{page_contents}</div>
      </div>
    )
  }
}
