import { OnApplicationBootstrap } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { ConfigService } from '@nestjs/config';
import { UsersService } from '../users/users.service.js';
import { RegisterDto } from './dto/register.dto.js';
import { LoginDto } from './dto/login.dto.js';
import { RefreshTokenDto } from './dto/refresh-token.dto.js';
import { ChangePasswordDto } from './dto/change-password.dto.js';
import { Role } from '../../common/enums/role.enum.js';
export interface AuthTokens {
    accessToken: string;
    refreshToken: string;
    expiresIn: string;
}
export declare class AuthService implements OnApplicationBootstrap {
    private readonly usersService;
    private readonly jwtService;
    private readonly configService;
    private readonly logger;
    constructor(usersService: UsersService, jwtService: JwtService, configService: ConfigService);
    onApplicationBootstrap(): Promise<void>;
    private seedDefaultAdmin;
    register(registerDto: RegisterDto): Promise<{
        user: {
            id: string;
            email: string;
            fullName: string;
            phone?: string;
            role: Role;
            isActive: boolean;
            createdAt: Date;
            updatedAt: Date;
        };
        tokens: AuthTokens;
    }>;
    login(loginDto: LoginDto): Promise<{
        user: {
            id: string;
            email: string;
            fullName: string;
            phone?: string;
            role: Role;
            isActive: boolean;
            createdAt: Date;
            updatedAt: Date;
        };
        tokens: AuthTokens;
    }>;
    refreshTokens(dto: RefreshTokenDto): Promise<AuthTokens>;
    logout(userId: string): Promise<{
        message: string;
    }>;
    changePassword(userId: string, dto: ChangePasswordDto): Promise<{
        message: string;
    }>;
    private generateTokens;
    private updateRefreshTokenHash;
}
