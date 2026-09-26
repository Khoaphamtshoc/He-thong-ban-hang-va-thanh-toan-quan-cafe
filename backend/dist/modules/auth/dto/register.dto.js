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
import { IsEmail, IsNotEmpty, IsOptional, IsString, MinLength, } from 'class-validator';
export class RegisterDto {
    email;
    password;
    fullName;
    phone;
}
__decorate([
    ApiProperty({ example: 'customer@gmail.com', description: 'Email đăng ký' }),
    IsEmail({}, { message: 'Email không đúng định dạng' }),
    IsNotEmpty({ message: 'Email không được để trống' }),
    __metadata("design:type", String)
], RegisterDto.prototype, "email", void 0);
__decorate([
    ApiProperty({ example: 'Password123!', description: 'Mật khẩu tối thiểu 6 ký tự' }),
    IsString(),
    MinLength(6, { message: 'Mật khẩu phải chứa ít nhất 6 ký tự' }),
    __metadata("design:type", String)
], RegisterDto.prototype, "password", void 0);
__decorate([
    ApiProperty({ example: 'Trần Văn Khách', description: 'Họ và tên người dùng' }),
    IsString(),
    IsNotEmpty({ message: 'Họ và tên không được để trống' }),
    __metadata("design:type", String)
], RegisterDto.prototype, "fullName", void 0);
__decorate([
    ApiPropertyOptional({ example: '0901234567', description: 'Số điện thoại' }),
    IsOptional(),
    IsString(),
    __metadata("design:type", String)
], RegisterDto.prototype, "phone", void 0);
//# sourceMappingURL=register.dto.js.map