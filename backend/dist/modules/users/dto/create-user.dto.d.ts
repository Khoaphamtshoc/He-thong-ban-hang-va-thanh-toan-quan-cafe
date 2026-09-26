import { Role } from '../../../common/enums/role.enum.js';
export declare class CreateUserDto {
    email: string;
    password: string;
    fullName: string;
    phone?: string;
    role?: Role;
}
