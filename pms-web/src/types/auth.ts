export interface LoginRequest {
  account: string
  password: string
}

export interface LoginResult {
  accessToken: string
  expiresAt: string
  personnelId: number
  personnelName: string
  roleType: string
  isAdmin: boolean
}
