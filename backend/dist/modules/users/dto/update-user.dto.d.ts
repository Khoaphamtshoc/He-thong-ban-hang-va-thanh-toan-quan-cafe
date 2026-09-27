import { Role } from '../../../common/enums/role.enum.js';
export declare class UpdateUserDto {
    fullName?: string;
    phone?: string;
    role?: Role;
    isActive?: boolean;
}
