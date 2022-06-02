import {API, ApiManager} from "./api_manager";
import {Main} from "../pages/Main";
import {
  PermissionInformationModel,
  UserDetailedInformationModel,
  UserInformationModel,
  UserUpdateModel
} from "../data_models";

class api_users {
  private readonly API: ApiManager

  constructor(apiManager: ApiManager) {
    this.API = apiManager;
  }

  public async get_users(): Promise<UserInformationModel[]> {
    const resp = await this.API.axs.get("/api/users/list")
    if (resp.status === 200) {
      return resp.data;
    } else {
      Main.app_error({error: `failed to update user list`, recoverable: false})
      return Promise<UserInformationModel[]>.reject();
    }
  }

  public async get_user(id: number): Promise<UserInformationModel> {
    const resp = await this.API.axs.get(`/api/users/${id}`)
    if (resp.status === 200) return resp.data;
    Main.app_error({error: `failed to get user`, recoverable: true})
    return Promise.reject()
  }

  public async get_permissions(): Promise<PermissionInformationModel[]> {
    const resp = await this.API.axs.get("/api/permission/list")
    if (resp.status === 200) {
      return resp.data;
    } else {
      Main.app_error({error: `failed to update permission models`, recoverable: false})
      return Promise<PermissionInformationModel[]>.reject();
    }
  }

  public async get_user_permissions(user: UserInformationModel): Promise<PermissionInformationModel[]> {
    const resp = await this.API.axs.get(`/api/permission/list/${user.userID}`)
    if (resp.status === 200) return resp.data
    Main.app_error({error: `failed to get user permissions`, recoverable: true})
    return Promise<PermissionInformationModel>.reject()
  }

  public async get_current_user(): Promise<UserDetailedInformationModel> {
    const resp = await this.API.axs.get(`/api/users/whoami`)
    if (resp.status === 200) return UserDetailedInformationModel.load_from_api(resp.data)
    Main.app_error({error: `failed to get current user?`, recoverable: true})
    return Promise<UserDetailedInformationModel>.reject()
  }

  public create_user_username: string = ""

  public async create_user(): Promise<UserInformationModel> {
    if (!this.create_user_username.length) return Promise.reject()
    const formData = new FormData();
    formData.append("username", this.create_user_username)
    const resp = await this.API.axs.post("/api/users/create", formData, {
      headers: {
        "Content-Type": "multipart/form-data"
      }
    })
    this.create_user_username = ""
    if (resp.status === 200) return resp.data
    Main.app_error({error: `failed to create new user`, recoverable: true})
    return Promise<UserInformationModel>.reject()
  }

  public async modify_user(user: UserDetailedInformationModel) {
    const edit_model: UserUpdateModel = {
      username: user.username,
      enabled: user.enabled,
      permissions: user.userPermissions
    }

    const resp = await this.API.axs.put(`/api/users/${user.userID}`, edit_model)
    if (resp.status === 200) return;
    Main.app_error({error: `failed to update user`, recoverable: true})
    return Promise.reject()
  }
}

export const API_USER: api_users = new api_users(API)
