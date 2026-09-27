var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { ApiPropertyOptional } from '@nestjs/swagger';
import { IsEnum, IsOptional, IsString } from 'class-validator';
import { Role } from '../../../common/enums/role.enum.js';
export class UpdateUserDto {
    fullName;
    phone;
    role;
    isActive;
}
__decorate([
    ApiPropertyOptional({ example: 'Nguyễn Văn B', description: 'Họ và tên' }),
    IsOptional(),
    IsString(),
    __metadata("design:type", String)
], UpdateUserDto.prototype, "fullName", void 0);
__decorate([
    ApiPropertyOptional({ example: '0912345678', description: 'Số điện thoại' }),
    IsOptional(),
    IsString(),
    __metadata("design:type", String)
], UpdateUserDto.prototype, "phone", void 0);
__decorate([
    ApiPropertyOptional({ enum: Role, description: 'Cập nhật quyền hạn (chỉ Admin)' }),
    IsOptional(),
    IsEnum(Role, { message: 'Vai trò không hợp lệ' }),
    __metadata("design:type", String)
], UpdateUserDto.prototype, "role", void 0);
__decorate([
    ApiPropertyOptional({ example: true, description: 'Trạng thái hoạt động' }),
    IsOptional(),
    __metadata("design:type", Boolean)
], UpdateUserDto.prototype, "isActive", void 0);
//# sourceMappingURL=update-user.dto.js.map