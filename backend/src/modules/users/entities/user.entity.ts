import {
  Entity,
  PrimaryGeneratedColumn,
  Column,
  CreateDateColumn,
  UpdateDateColumn,
} from 'typeorm';
import { Exclude } from 'class-transformer';
import { Role } from '../../../common/enums/role.enum.js';

@Entity('users')
export class User {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ unique: true, length: 150 })
  email: string;

  @Column({ select: false })
  @Exclude()
  password: string;

  @Column({ length: 100 })
  fullName: string;

  @Column({ nullable: true, length: 20 })
  phone?: string;

  @Column({
    type: 'varchar',
    length: 20,
    default: Role.CUSTOMER,
  })
  role: Role;

  @Column({ nullable: true, select: false })
  @Exclude()
  refreshTokenHash?: string;

  @Column({ default: true })
  isActive: boolean;

  @CreateDateColumn()
  createdAt: Date;

  @UpdateDateColumn()
  updatedAt: Date;
}
