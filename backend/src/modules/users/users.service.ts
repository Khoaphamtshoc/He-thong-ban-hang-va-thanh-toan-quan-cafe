import {
  ConflictException,
  Injectable,
  NotFoundException,
} from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { User } from './entities/user.entity.js';
import { CreateUserDto } from './dto/create-user.dto.js';
import { UpdateUserDto } from './dto/update-user.dto.js';

@Injectable()
export class UsersService {
  constructor(
    @InjectRepository(User)
    private readonly userRepository: Repository<User>,
  ) {}

  async create(createUserDto: CreateUserDto & { password: string }): Promise<User> {
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

  async findAll(): Promise<User[]> {
    return await this.userRepository.find({
      order: { createdAt: 'DESC' },
    });
  }

  async findById(id: string): Promise<User> {
    const user = await this.userRepository.findOne({ where: { id } });
    if (!user) {
      throw new NotFoundException(`Không tìm thấy người dùng với ID: ${id}`);
    }
    return user;
  }

  async findByEmail(email: string, includeSecrets = false): Promise<User | null> {
    const query = this.userRepository
      .createQueryBuilder('user')
      .where('user.email = :email', { email: email.toLowerCase().trim() });

    if (includeSecrets) {
      query.addSelect('user.password').addSelect('user.refreshTokenHash');
    }

    return await query.getOne();
  }

  async findByIdWithSecrets(id: string): Promise<User | null> {
    return await this.userRepository
      .createQueryBuilder('user')
      .addSelect('user.password')
      .addSelect('user.refreshTokenHash')
      .where('user.id = :id', { id })
      .getOne();
  }

  async update(id: string, updateUserDto: UpdateUserDto): Promise<User> {
    const user = await this.findById(id);
    Object.assign(user, updateUserDto);
    return await this.userRepository.save(user);
  }

  async updateRefreshToken(id: string, refreshTokenHash: string | null): Promise<void> {
    await this.userRepository.update(id, {
      refreshTokenHash: refreshTokenHash ?? undefined,
    });
  }

  async count(): Promise<number> {
    return await this.userRepository.count();
  }
}
