import { Repository } from 'typeorm';
import { User } from './entities/user.entity.js';
import { CreateUserDto } from './dto/create-user.dto.js';
import { UpdateUserDto } from './dto/update-user.dto.js';
export declare class UsersService {
    private readonly userRepository;
    constructor(userRepository: Repository<User>);
    create(createUserDto: CreateUserDto & {
        password: string;
    }): Promise<User>;
    findAll(): Promise<User[]>;
    findById(id: string): Promise<User>;
    findByEmail(email: string, includeSecrets?: boolean): Promise<User | null>;
    findByIdWithSecrets(id: string): Promise<User | null>;
    update(id: string, updateUserDto: UpdateUserDto): Promise<User>;
    updateRefreshToken(id: string, refreshTokenHash: string | null): Promise<void>;
    count(): Promise<number>;
}
