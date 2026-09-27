var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
import { Body, Controller, Get, HttpCode, HttpStatus, Patch, Post, UseGuards, } from '@nestjs/common';
import { ApiBearerAuth, ApiOperation, ApiResponse, ApiTags, } from '@nestjs/swagger';
import { AuthService } from './auth.service.js';
import { RegisterDto } from './dto/register.dto.js';
import { LoginDto } from './dto/login.dto.js';
import { RefreshTokenDto } from './dto/refresh-token.dto.js';
import { ChangePasswordDto } from './dto/change-password.dto.js';
import { Public } from '../../common/decorators/public.decorator.js';
import { CurrentUser } from '../../common/decorators/current-user.decorator.js';
import { JwtAuthGuard } from '../../common/guards/jwt-auth.guard.js';
import { RolesGuard } from '../../common/guards/roles.guard.js';
import { Roles } from '../../common/decorators/roles.decorator.js';
import { Role } from '../../common/enums/role.enum.js';
import { User } from '../users/entities/user.entity.js';
let AuthController = class AuthController {
    authService;
    constructor(authService) {
        this.authService = authService;
    }
    async register(registerDto) {
        const result = await this.authService.register(registerDto);
        return {
            message: 'Đăng ký tài khoản thành công',
            data: result,
        };
    }
    async login(loginDto) {
        const result = await this.authService.login(loginDto);
        return {
            message: 'Đăng nhập thành công',
            data: result,
        };
    }
    async refresh(refreshTokenDto) {
        const tokens = await this.authService.refreshTokens(refreshTokenDto);
        return {
            message: 'Cấp lại token thành công',
            data: tokens,
        };
    }
    async logout(userId) {
        const result = await this.authService.logout(userId);
        return {
            message: result.message,
            data: null,
        };
    }
    async getProfile(user) {
        return {
            message: 'Lấy thông tin tài khoản thành công',
            data: user,
        };
    }
    async changePassword(userId, changePasswordDto) {
        const result = await this.authService.changePassword(userId, changePasswordDto);
        return {
            message: result.message,
            data: null,
        };
    }
    testAdminRoute(user) {
        return {
            message: 'Xác thực phân quyền thành công! Bạn có quyền ADMIN tối cao.',
            data: {
                adminId: user.id,
                email: user.email,
                role: user.role,
            },
        };
    }
};
__decorate([
    Public(),
    Post('register'),
    HttpCode(HttpStatus.CREATED),
    ApiOperation({ summary: 'Đăng ký tài khoản khách hàng mới' }),
    ApiResponse({ status: 201, description: 'Đăng ký thành công, trả về thông tin user và cặp token' }),
    ApiResponse({ status: 409, description: 'Email đã tồn tại' }),
    __param(0, Body()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [RegisterDto]),
    __metadata("design:returntype", Promise)
], AuthController.prototype, "register", null);
__decorate([
    Public(),
    Post('login'),
    HttpCode(HttpStatus.OK),
    ApiOperation({ summary: 'Đăng nhập hệ thống (Lấy Access Token & Refresh Token)' }),
    ApiResponse({ status: 200, description: 'Đăng nhập thành công' }),
    ApiResponse({ status: 401, description: 'Sai email hoặc mật khẩu' }),
    __param(0, Body()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [LoginDto]),
    __metadata("design:returntype", Promise)
], AuthController.prototype, "login", null);
__decorate([
    Public(),
    Post('refresh'),
    HttpCode(HttpStatus.OK),
    ApiOperation({ summary: 'Làm mới Access Token bằng Refresh Token' }),
    ApiResponse({ status: 200, description: 'Cấp mới token thành công' }),
    ApiResponse({ status: 403, description: 'Refresh token không hợp lệ hoặc đã hết hạn' }),
    __param(0, Body()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [RefreshTokenDto]),
    __metadata("design:returntype", Promise)
], AuthController.prototype, "refresh", null);
__decorate([
    UseGuards(JwtAuthGuard),
    ApiBearerAuth('JWT-auth'),
    Post('logout'),
    HttpCode(HttpStatus.OK),
    ApiOperation({ summary: 'Đăng xuất tài khoản (Thu hồi Refresh Token)' }),
    __param(0, CurrentUser('id')),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [String]),
    __metadata("design:returntype", Promise)
], AuthController.prototype, "logout", null);
__decorate([
    UseGuards(JwtAuthGuard),
    ApiBearerAuth('JWT-auth'),
    Get('me'),
    ApiOperation({ summary: 'Lấy thông tin tài khoản đang đăng nhập' }),
    __param(0, CurrentUser()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [User]),
    __metadata("design:returntype", Promise)
], AuthController.prototype, "getProfile", null);
__decorate([
    UseGuards(JwtAuthGuard),
    ApiBearerAuth('JWT-auth'),
    Patch('change-password'),
    ApiOperation({ summary: 'Đổi mật khẩu' }),
    __param(0, CurrentUser('id')),
    __param(1, Body()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [String, ChangePasswordDto]),
    __metadata("design:returntype", Promise)
], AuthController.prototype, "changePassword", null);
__decorate([
    UseGuards(JwtAuthGuard, RolesGuard),
    Roles(Role.ADMIN),
    ApiBearerAuth('JWT-auth'),
    Get('admin-test'),
    ApiOperation({ summary: 'API kiểm tra RBAC (Chỉ Admin mới có quyền truy cập)' }),
    __param(0, CurrentUser()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [User]),
    __metadata("design:returntype", void 0)
], AuthController.prototype, "testAdminRoute", null);
AuthController = __decorate([
    ApiTags('Xác thực & Phân quyền (Auth)'),
    Controller('auth'),
    __metadata("design:paramtypes", [AuthService])
], AuthController);
export { AuthController };
//# sourceMappingURL=auth.controller.js.map