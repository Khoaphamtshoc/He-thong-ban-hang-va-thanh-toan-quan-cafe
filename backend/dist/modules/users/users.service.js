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
import { ConflictException, Injectable, NotFoundException, } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { User } from './entities/user.entity.js';
let UsersService = class UsersService {
    userRepository;
    constructor(userRepository) {
        this.userRepository = userRepository;
    }
    async create(createUserDto) {
        const existing = await this.userRepository.findOne({
            where: { email: createUserDto.email.toLowerCase().trim() },
        });
        if (existing) {
            throw new ConflictException('Email này đã được sử dụng');
        }
        const user = this.userRepository.create({
            ...createUserDto,
            email: createUserDto.email.toLowerCase().trim(),
        });
        return await this.userRepository.save(user);
    }
    async findAll() {
        return await this.userRepository.find({
            order: { createdAt: 'DESC' },
        });
    }
    async findById(id) {
        const user = await this.userRepository.findOne({ where: { id } });
        if (!user) {
            throw new NotFoundException(`Không tìm thấy người dùng với ID: ${id}`);
        }
        return user;
    }
    async findByEmail(email, includeSecrets = false) {
        const query = this.userRepository
            .createQueryBuilder('user')
            .where('user.email = :email', { email: email.toLowerCase().trim() });
        if (includeSecrets) {
            query.addSelect('user.password').addSelect('user.refreshTokenHash');
        }
        return await query.getOne();
    }
    async findByIdWithSecrets(id) {
        return await this.userRepository
            .createQueryBuilder('user')
            .addSelect('user.password')
            .addSelect('user.refreshTokenHash')
            .where('user.id = :id', { id })
            .getOne();
    }
    async update(id, updateUserDto) {
        const user = await this.findById(id);
        Object.assign(user, updateUserDto);
        return await this.userRepository.save(user);
    }
    async updateRefreshToken(id, refreshTokenHash) {
        await this.userRepository.update(id, {
            refreshTokenHash: refreshTokenHash ?? undefined,
        });
    }
    async count() {
        return await this.userRepository.count();
    }
};
UsersService = __decorate([
    Injectable(),
    __param(0, InjectRepository(User)),
    __metadata("design:paramtypes", [Repository])
], UsersService);
export { UsersService };
//# sourceMappingURL=users.service.js.map