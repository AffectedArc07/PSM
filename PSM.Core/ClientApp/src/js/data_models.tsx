import {onlyUnique} from "./components/helpers";
import {API_USER} from "./core/api_users";
import Permissions from "./helpers/permissions";

export class UserInformationModel {
  username: string
  userID: number
  enabled: boolean
}

export class PermissionInformationModel {
  name: string
  id: number
  description: string
}

export class UserDetailedInformationModel {
  username: string
  userID: number
  userPermissions: string
  enabled: boolean

  public static async load_from_api(user: UserInformationModel): Promise<UserDetailedInformationModel> {
    const perms = await API_USER.get_user_permissions(user)
    const user_details = await API_USER.get_user(user.userID)
    console.log(`loading from api`)
    console.log(user_details)
    const model = new UserDetailedInformationModel();
    model.username = user_details.username
    model.userID = user_details.userID
    model.enabled = user_details.enabled
    model.set_perms_from_list(perms.map(perm => perm.id).sort())
    return model
  }

  public as_user_model(): UserInformationModel {
    return {
      username: this.username,
      userID: this.userID,
      enabled: this.enabled,
    }
  }

  private perms_as_list(): number[] {
    const permArray: number[] = []
    this.userPermissions.split(";").filter(onlyUnique).forEach(perm => permArray.push(Number.parseInt(perm)))
    return permArray
  }

  private set_perms_from_list(perms: number[]): void {
    let new_perms: string | undefined = undefined
    perms.sort().filter(onlyUnique).forEach(perm => {
      new_perms = new_perms ? `${new_perms};${perm}` : `${perm}`
    })
    this.userPermissions = new_perms ? new_perms : ''
  }

  permission_check(permission: number): boolean {
    return this.perms_as_list().includes(permission)
  }

  permission_add(permission: number): void {
    if (this.permission_check(permission)) return
    const current = this.perms_as_list()
    current.push(permission)
    this.set_perms_from_list(current)
  }

  permission_remove(permission: number): void {
    if (!this.permission_check(permission)) return
    const current = this.perms_as_list()
    current.splice(current.indexOf(permission), 1)
    this.set_perms_from_list(current)
  }

  is_user_data_editable(user: UserDetailedInformationModel) {
    if (this.userID < 3) return false;
    return user.permission_check(Permissions.UserModify);
  }
}

export class UserUpdateModel {
  username: string
  enabled: boolean
  permissions: string
}
