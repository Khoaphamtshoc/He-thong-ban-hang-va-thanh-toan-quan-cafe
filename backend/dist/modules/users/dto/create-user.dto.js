var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { IsEmail, IsEnum, IsNotEmpty, IsOptional, IsString, MinLength, } from 'class-validator';
import { Role } from '../../../common/enums/role.enum.js';
export class CreateUserDto {
    email;
    password;
    fullName;
    phone;
    role;
}
__decorate([
    ApiProperty({ example: 'nguyenvana@gmail.com', description: 'Email của người dùng' }),
    IsEmail({}, { message: 'Email không đúng định dạng' }),
    IsNotEmpty({ message: 'Email không được để trống' }),
    __metadata("design:type", String)
], CreateUserDto.prototype, "email", void 0);
__decorate([
    ApiProperty({ example: 'Password123!', description: 'Mật khẩu tối thiểu 6 ký tự' }),
    IsString(),
    MinLength(6, { message: 'Mật khẩu phải chứa ít nhất 6 ký tự' }),
    __metadata("design:type", String)
], CreateUserDto.prototype, "password", void 0);
__decorate([
    ApiProperty({ example: 'Nguyễn Văn A', description: 'Họ và tên' }),
    IsString(),
    IsNotEmpty({ message: 'Họ và tên không được để trống' }),
    __metadata("design:type", String)
], CreateUserDto.prototype, "fullName", void 0);
__decorate([
    ApiPropertyOptional({ example: '0987654321', description: 'Số điện thoại' }),
    IsOptional(),
    IsString(),
    __metadata("design:type", String)
], CreateUserDto.prototype, "phone", void 0);
__decorate([
    ApiPropertyOptional({ enum: Role, default: Role.CUSTOMER, description: 'Vai trò người dùng' }),
    IsOptional(),
    IsEnum(Role, { message: 'Vai trò không hợp lệ' }),
    __metadata("design:type", String)
], CreateUserDto.prototype, "role", void 0);
//# sourceMappingURL=create-user.dto.js.map