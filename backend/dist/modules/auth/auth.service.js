var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var AuthService_1;
import { BadRequestException, ForbiddenException, Injectable, Logger, UnauthorizedException, } from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import { ConfigService } from '@nestjs/config';
import * as bcrypt from 'bcrypt';
import { UsersService } from '../users/users.service.js';
import { Role } from '../../common/enums/role.enum.js';
let AuthService = AuthService_1 = class AuthService {
    usersService;
    jwtService;
    configService;
    logger = new Logger(AuthService_1.name);
    constructor(usersService, jwtService, configService) {
        this.usersService = usersService;
        this.jwtService = jwtService;
        this.configService = configService;
    }
    async onApplicationBootstrap() {
        await this.seedDefaultAdmin();
    }
    async seedDefaultAdmin() {
        try {
            const userCount = await this.usersService.count();
            if (userCount === 0) {
                const adminEmail = 'admin@moc.coffee';
                const rawPassword = 'Admin@123456';
                const hashedPassword = await bcrypt.hash(rawPassword, 10);
                await this.usersService.create({
                    email: adminEmail,
                    password: hashedPassword,
                    fullName: 'Quản trị viên Hệ thống',
                    phone: '0988888888',
                    role: Role.ADMIN,
                });
                this.logger.log(`[SEED] Tạo thành công tài khoản Quản trị viên: ${adminEmail} / ${rawPassword}`);
            }
        }
        catch (error) {
            this.logger.error('Lỗi khi seed tài khoản Quản trị viên mặc định', error);
        }
    }
    async register(registerDto) {
        const hashedPassword = await bcrypt.hash(registerDto.password, 10);
        const user = await this.usersService.create({
            ...registerDto,
            password: hashedPassword,
            role: Role.CUSTOMER,
        });
        const tokens = await this.generateTokens(user);
        await this.updateRefreshTokenHash(user.id, tokens.refreshToken);
        const { password, refreshTokenHash, ...userResponse } = user;
        return {
            user: userResponse,
            tokens,
        };
    }
    async login(loginDto) {
        const user = await this.usersService.findByEmail(loginDto.email, true);
        if (!user) {
            throw new UnauthorizedException('Email hoặc mật khẩu không chính xác');
        }
        if (!user.isActive) {
            throw new ForbiddenException('Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Admin.');
        }
        const isMatch = await bcrypt.compare(loginDto.password, user.password);
        if (!isMatch) {
            throw new UnauthorizedException('Email hoặc mật khẩu không chính xác');
        }
        const tokens = await this.generateTokens(user);
        await this.updateRefreshTokenHash(user.id, tokens.refreshToken);
        const { password, refreshTokenHash, ...userResponse } = user;
        return {
            user: userResponse,
            tokens,
        };
    }
    async refreshTokens(dto) {
        let payload;
        try {
            payload = await this.jwtService.verifyAsync(dto.refreshToken, {
                secret: this.configService.get('JWT_REFRESH_SECRET') || 'default_refresh_secret',
            });
        }
        catch {
            throw new ForbiddenException('Refresh token không hợp lệ hoặc đã hết hạn');
        }
        const user = await this.usersService.findByIdWithSecrets(payload.sub);
        if (!user || !user.refreshTokenHash || !user.isActive) {
            throw new ForbiddenException('Truy cập bị từ chối');
        }
        const refreshTokenMatches = await bcrypt.compare(dto.refreshToken, user.refreshTokenHash);
        if (!refreshTokenMatches) {
            throw new ForbiddenException('Refresh token không hợp lệ');
        }
        const tokens = await this.generateTokens(user);
        await this.updateRefreshTokenHash(user.id, tokens.refreshToken);
        return tokens;
    }
    async logout(userId) {
        await this.usersService.updateRefreshToken(userId, null);
        return { message: 'Đăng xuất thành công' };
    }
    async changePassword(userId, dto) {
        const user = await this.usersService.findByIdWithSecrets(userId);
        if (!user) {
            throw new UnauthorizedException('Không tìm thấy người dùng');
        }
        const isMatch = await bcrypt.compare(dto.oldPassword, user.password);
        if (!isMatch) {
            throw new BadRequestException('Mật khẩu hiện tại không đúng');
        }
        if (dto.oldPassword === dto.newPassword) {
            throw new BadRequestException('Mật khẩu mới không được trùng với mật khẩu cũ');
        }
        const hashedPassword = await bcrypt.hash(dto.newPassword, 10);
        await this.usersService.update(userId, {
            ...user,
            password: hashedPassword,
        });
        return { message: 'Đổi mật khẩu thành công. Vui lòng đăng nhập lại.' };
    }
    async generateTokens(user) {
        const payload = {
            sub: user.id,
            email: user.email,
            role: user.role,
        };
        const accessExpiresIn = this.configService.get('JWT_ACCESS_EXPIRES_IN') || '15m';
        const refreshExpiresIn = this.configService.get('JWT_REFRESH_EXPIRES_IN') || '7d';
        const [accessToken, refreshToken] = await Promise.all([
            this.jwtService.signAsync(payload, {
                secret: this.configService.get('JWT_ACCESS_SECRET') || 'default_access_secret',
                expiresIn: accessExpiresIn,
            }),
            this.jwtService.signAsync(payload, {
                secret: this.configService.get('JWT_REFRESH_SECRET') || 'default_refresh_secret',
                expiresIn: refreshExpiresIn,
            }),
        ]);
        return {
            accessToken,
            refreshToken,
            expiresIn: accessExpiresIn,
        };
    }
    async updateRefreshTokenHash(userId, refreshToken) {
        const hash = await bcrypt.hash(refreshToken, 10);
        await this.usersService.updateRefreshToken(userId, hash);
    }
};
AuthService = AuthService_1 = __decorate([
    Injectable(),
    __metadata("design:paramtypes", [UsersService,
        JwtService,
        ConfigService])
], AuthService);
export { AuthService };
//# sourceMappingURL=auth.service.js.map