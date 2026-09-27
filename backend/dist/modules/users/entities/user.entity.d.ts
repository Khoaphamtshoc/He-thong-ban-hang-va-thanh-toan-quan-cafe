import { Role } from '../../../common/enums/role.enum.js';
export declare class User {
    id: string;
    email: string;
    password: string;
    fullName: string;
    phone?: string;
    role: Role;
    refreshTokenHash?: string;
    isActive: boolean;
    createdAt: Date;
    updatedAt: Date;
}
