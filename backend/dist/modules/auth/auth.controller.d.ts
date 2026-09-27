import { AuthService } from './auth.service.js';
import { RegisterDto } from './dto/register.dto.js';
import { LoginDto } from './dto/login.dto.js';
import { RefreshTokenDto } from './dto/refresh-token.dto.js';
import { ChangePasswordDto } from './dto/change-password.dto.js';
import { Role } from '../../common/enums/role.enum.js';
import { User } from '../users/entities/user.entity.js';
export declare class AuthController {
    private readonly authService;
    constructor(authService: AuthService);
    register(registerDto: RegisterDto): Promise<{
        message: string;
        data: {
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
            tokens: import("./auth.service.js").AuthTokens;
        };
    }>;
    login(loginDto: LoginDto): Promise<{
        message: string;
        data: {
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
            tokens: import("./auth.service.js").AuthTokens;
        };
    }>;
    refresh(refreshTokenDto: RefreshTokenDto): Promise<{
        message: string;
        data: import("./auth.service.js").AuthTokens;
    }>;
    logout(userId: string): Promise<{
        message: string;
        data: null;
    }>;
    getProfile(user: User): Promise<{
        message: string;
        data: User;
    }>;
    changePassword(userId: string, changePasswordDto: ChangePasswordDto): Promise<{
        message: string;
        data: null;
    }>;
    testAdminRoute(user: User): {
        message: string;
        data: {
            adminId: string;
            email: string;
            role: Role;
        };
    };
}
