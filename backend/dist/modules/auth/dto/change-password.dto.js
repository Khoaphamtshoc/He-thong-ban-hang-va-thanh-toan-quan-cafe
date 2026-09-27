var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { ApiProperty } from '@nestjs/swagger';
import { IsNotEmpty, IsString, MinLength } from 'class-validator';
export class ChangePasswordDto {
    oldPassword;
    newPassword;
}
__decorate([
    ApiProperty({ example: 'OldPassword123!', description: 'Mật khẩu hiện tại' }),
    IsString(),
    IsNotEmpty({ message: 'Mật khẩu hiện tại không được để trống' }),
    __metadata("design:type", String)
], ChangePasswordDto.prototype, "oldPassword", void 0);
__decorate([
    ApiProperty({ example: 'NewPassword123!', description: 'Mật khẩu mới tối thiểu 6 ký tự' }),
    IsString(),
    MinLength(6, { message: 'Mật khẩu mới phải chứa ít nhất 6 ký tự' }),
    __metadata("design:type", String)
], ChangePasswordDto.prototype, "newPassword", void 0);
//# sourceMappingURL=change-password.dto.js.map