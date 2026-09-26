var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
import { Entity, PrimaryGeneratedColumn, Column, CreateDateColumn, UpdateDateColumn, } from 'typeorm';
import { Exclude } from 'class-transformer';
import { Role } from '../../../common/enums/role.enum.js';
let User = class User {
    id;
    email;
    password;
    fullName;
    phone;
    role;
    refreshTokenHash;
    isActive;
    createdAt;
    updatedAt;
};
__decorate([
    PrimaryGeneratedColumn('uuid'),
    __metadata("design:type", String)
], User.prototype, "id", void 0);
__decorate([
    Column({ unique: true, length: 150 }),
    __metadata("design:type", String)
], User.prototype, "email", void 0);
__decorate([
    Column({ select: false }),
    Exclude(),
    __metadata("design:type", String)
], User.prototype, "password", void 0);
__decorate([
    Column({ length: 100 }),
    __metadata("design:type", String)
], User.prototype, "fullName", void 0);
__decorate([
    Column({ nullable: true, length: 20 }),
    __metadata("design:type", String)
], User.prototype, "phone", void 0);
__decorate([
    Column({
        type: 'varchar',
        length: 20,
        default: Role.CUSTOMER,
    }),
    __metadata("design:type", String)
], User.prototype, "role", void 0);
__decorate([
    Column({ nullable: true, select: false }),
    Exclude(),
    __metadata("design:type", String)
], User.prototype, "refreshTokenHash", void 0);
__decorate([
    Column({ default: true }),
    __metadata("design:type", Boolean)
], User.prototype, "isActive", void 0);
__decorate([
    CreateDateColumn(),
    __metadata("design:type", Date)
], User.prototype, "createdAt", void 0);
__decorate([
    UpdateDateColumn(),
    __metadata("design:type", Date)
], User.prototype, "updatedAt", void 0);
User = __decorate([
    Entity('users')
], User);
export { User };
//# sourceMappingURL=user.entity.js.map