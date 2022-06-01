import {ApiManager} from "./api_manager";
import {Main} from "../pages/Main";

export type UserInformation = {
  username: string
  userID: number
  enabled: boolean
  permissions?: UserPermissions
}

export type UserPermissions = {}

export type UserPermissionModal = {
  name: string
  id: number
  description: string
}

export default class api_users {
  private readonly API: ApiManager

  public permission_modals: UserPermissionModal[] = []

  public user_list: UserInformation[] = []

  constructor(apiManager: ApiManager) {
    this.API = apiManager;
  }

  public async get_users(): Promise<void> {
    const resp = await this.API.axs.get("/api/users/list")
    if (resp.status === 200) {
      this.user_list = resp.data;
    } else {
      Main.app_error({error: `failed to update user list`, recoverable: false})
      return Promise<void>.reject();
    }
  }

  public async get_permission_modals(): Promise<void> {
    // let anything accessing modals know that they are possibly outdated
    const resp = await this.API.axs.get("/api/permission/list")
    if (resp.status === 200) {
      this.permission_modals = resp.data;
    } else {
      Main.app_error({error: `failed to update permission modals`, recoverable: false})
      return Promise<void>.reject();
    }
  }
}
