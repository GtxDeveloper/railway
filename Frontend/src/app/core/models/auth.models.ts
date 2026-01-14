export interface RegisterRequest{
  name: string,
  surname: string,
  email: string,
  password: string,
  brand: string,
  phone: string,
  city: string,
  inviteToken: string
}

export interface VerifyRequest{
  email: string,
  code: string,
}

export interface LoginRequest{
  email: string,
  password: string,
}

export interface RefreshRequest{
  accessToken: string,
  refreshToken: string,
}

export interface AuthResponse{
  token: string,
  refreshToken: string,
  userId: string,
  email: string,
}

export interface User{
  id: string;
  email: string;
  role: string;
  exp: number;
}
