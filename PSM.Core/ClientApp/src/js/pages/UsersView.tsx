import React from 'react';
import {API_USER} from "../core/api_users";
import NavigationBar, {NavigationButton} from "../components/navigation_bar";
import {PermissionInformationModel, UserDetailedInformationModel, UserInformationModel} from "../data_models";
import Permissions from "../helpers/permissions";

type UsersViewProps = {}

type UsersViewState = {
  user_list_enabled: UserInformationModel[]
  user_list_archived: UserInformationModel[]

  active_user: UserInformationModel
  active_page: number
  permission_modals: PermissionInformationModel[]
  edit_user_data: UserDetailedInformationModel
  edit_user_original: UserDetailedInformationModel
  logged_in_user: UserDetailedInformationModel
}

const PageConstants = {
  DEFAULT_PAGE: 1,
  CREATE_USER: 2,
  EDIT_USER: 3,
}

export class UsersView extends React.Component<UsersViewProps, UsersViewState> {
  setState<K extends keyof UsersViewState>(state: ((prevState: Readonly<UsersViewState>, props: Readonly<UsersViewProps>) => (Pick<UsersViewState, K> | UsersViewState | null)) | Pick<UsersViewState, K> | UsersViewState | null, callback?: () => void) {
    super.setState(state, callback);
  }

  componentDidMount() {
    if (!this.state?.active_page)
      this.setState({active_page: PageConstants.DEFAULT_PAGE})
    this.start_api_update();
  }

  public start_api_update() {
    Promise.all([
      API_USER.get_enabled_users(),
      API_USER.get_archived_users(),
      API_USER.get_permissions(),
      API_USER.get_current_user(),
    ]).then(([enabled_users, disabled_users, permissions, current_user]) => {
      this.setState({
        user_list_enabled: enabled_users,
        user_list_archived: disabled_users,
        permission_modals: permissions,
        logged_in_user: current_user,
      })
    })
  }

  private nav_from_user(user: UserInformationModel | undefined, buttons: NavigationButton[]) {
    if (!user) return undefined
    return buttons.find(button => button.name === user.username)
  }

  private nav_buttons(): JSX.Element[] {
    const cancel = <button key='cancel_button'
                           onClick={() => this.setState({active_page: PageConstants.DEFAULT_PAGE})}>Cancel</button>
    const default_user_actions = [
      <button
        key='user_button_edit'
        disabled={!this.state.logged_in_user.permission_check(Permissions.UserModify)}
        onClick={() => {
          UserDetailedInformationModel.load_from_api(this.state.active_user).then(entry => {
            this.setState({
              active_page: PageConstants.EDIT_USER,
              edit_user_original: entry,
              edit_user_data: entry.clone()
            })
          })
        }}>
        Edit User
      </button>,
      <button
        key='user_button_create'
        disabled={!this.state.logged_in_user.permission_check(Permissions.UserCreate)}
        onClick={() => this.setState({active_page: PageConstants.CREATE_USER})}>
        Create User
      </button>
    ]
    const edit_user_actions = [
      <button
        key='user_edit_submit'
        disabled={!this.do_user_edit_check_changes()}
        onClick={() => this.do_user_edit_submit()}>
        Submit User Changes
      </button>,
      <button
        key='user_edit_revert'
        disabled={!this.do_user_edit_check_changes()}
        onClick={() => this.do_user_edit_revert()}>
        Revert User Changes
      </button>,
      <button
        key='user_edit_archive'
        disabled={!this.state.logged_in_user.permission_check(Permissions.UserDisable)}
        onClick={() => this.do_user_edit_archive()}>
        <b>Archive User</b>
      </button>
    ]
    let ret: JSX.Element[] = [];
    switch (this.state?.active_page) {
      case PageConstants.EDIT_USER:
        edit_user_actions.forEach((entry) => ret.push(entry))
        break
      case PageConstants.DEFAULT_PAGE:
        if (this.state?.active_user)
          default_user_actions.forEach((entry) => ret.push(entry))
        break
    }
    if (this.state?.active_page !== PageConstants.DEFAULT_PAGE)
      ret.push(cancel)
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
    return (<form>
      <input value={API_USER.create_user_username} onChange={(input => {
        API_USER.create_user_username = input.target.value?.length ? input.target.value : ""
        this.forceUpdate()
      })}/>
      <input disabled={!API_USER.create_user_username.length} type={"button"} value={"Create User"} onClick={() => {
        API_USER.create_user().then((new_user) => {
          this.start_api_update()
          this.setState({active_user: new_user, active_page: PageConstants.EDIT_USER})
        })
      }}/>
    </form>)
  }

  private render_edit_user(): JSX.Element {
    let user_data: UserDetailedInformationModel = this.state.edit_user_data
    if (!user_data || user_data.userID !== this.state.edit_user_original.userID) {
      this.setState({edit_user_data: this.state.edit_user_original.clone()})
      return <p>Loading</p>
    }
    const data_editable = user_data.is_user_data_editable(this.state.logged_in_user)
    console.log(`editable: ${data_editable}`)
    console.log(`user can rename: ${this.state.logged_in_user.permission_check(Permissions.UserRename)}`)

    return (<div>
        <p>Username: <input
          disabled={!data_editable || !this.state.logged_in_user.permission_check(Permissions.UserRename)}
          readOnly={!data_editable}
          defaultValue={user_data.username}
          onChange={(value) => {
            user_data.username = value.target.value
            this.setState({edit_user_data: user_data})
          }}/></p>
        <p>Enabled: <input type={"checkbox"}
                           disabled={!data_editable || !this.state.logged_in_user.permission_check(Permissions.UserEnable)}
                           readOnly={!data_editable}
                           checked={user_data.enabled}
                           onChange={(value) => {
                             user_data.enabled = value.target.checked
                             this.setState({edit_user_data: user_data})
                           }}/></p>
        <p>PermissionString: <input disabled={true} readOnly={true} value={user_data.userPermissions}/></p>
        <div title={"Permissions"}>{this.state.permission_modals.map(permission_modal => (
          <p key={permission_modal.name}>{permission_modal.name} <input type={"checkbox"}
                                                                        checked={user_data.permission_check(permission_modal.id)}
                                                                        readOnly={!data_editable}
                                                                        disabled={!data_editable || !this.state.logged_in_user.permission_check(permission_modal.id)}
                                                                        title={permission_modal.description}
                                                                        onChange={(value) => {
                                                                          if (value.target.checked)
                                                                            user_data.permission_add(permission_modal.id)
                                                                          else
                                                                            user_data.permission_remove(permission_modal.id)
                                                                          this.setState({edit_user_data: user_data})
                                                                        }}/>
          </p>
        ))}</div>
      </div>
    )
  }

  private do_user_edit_check_changes() {
    const current = this.state.edit_user_data
    const actual = this.state.edit_user_original
    console.log(current)
    console.log(actual)
    for (const key in current)
      if (current[key] !== actual[key])
        return true
    return false
  }

  private do_user_edit_revert() {
    UserDetailedInformationModel.load_from_api(this.state.active_user).then((value) => {
      this.setState({edit_user_data: value})
    })
  }

  private do_user_edit_archive() {
    if (!window.confirm(`CONFIRM YOU WANT TO ARCHIVE USER`))
      return
    API_USER.toggle_user_archive(this.state.active_user).then((archived) => {
      alert(archived ? `User Archived` : `User Restored`)
      this.start_api_update()
      this.setState({active_page: PageConstants.DEFAULT_PAGE})
    })
  }

  private do_user_edit_submit() {
    API_USER.modify_user(this.state.edit_user_data).then(() => {
      this.start_api_update()
      API_USER.get_user(this.state.active_user.userID).then((value => {
        this.setState({active_user: value})
      }))
      this.do_user_edit_revert() // it works okay?
    })
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
    if (!this.state?.logged_in_user) {
      return <p>Failed to retrieve logged in user</p>
    }
    if (!this.state?.user_list_enabled || this.state.user_list_enabled.length === 0) {
      return (
        <p>Loading</p>
      )
    }
    const nav_map = this.state.user_list_enabled.map(user => {
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
    const title_user = this.state?.active_user ? `${this.state.active_user.username} [${this.state.active_user.userID}]` : `No User Selected`
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
