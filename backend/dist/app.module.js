var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { UsersModule } from './modules/users/users.module.js';
import { AuthModule } from './modules/auth/auth.module.js';
let AppModule = class AppModule {
};
AppModule = __decorate([
    Module({
        imports: [
            ConfigModule.forRoot({
                isGlobal: true,
                envFilePath: '.env',
            }),
            TypeOrmModule.forRootAsync({
                imports: [ConfigModule],
                inject: [ConfigService],
                useFactory: (configService) => {
                    const dbType = configService.get('DB_TYPE', 'sqlite');
                    if (dbType === 'postgres') {
                        return {
                            type: 'postgres',
                            host: configService.get('DB_HOST', 'localhost'),
                            port: Number(configService.get('DB_PORT', 5432)),
                            username: configService.get('DB_USERNAME', 'postgres'),
                            password: configService.get('DB_PASSWORD', 'postgres'),
                            database: configService.get('DB_NAME', 'cafe_pos'),
                            autoLoadEntities: true,
                            synchronize: true,
                        };
                    }
                    return {
                        type: 'better-sqlite3',
                        database: configService.get('DB_DATABASE', 'database.sqlite'),
                        autoLoadEntities: true,
                        synchronize: true,
                    };
                },
            }),
            UsersModule,
            AuthModule,
        ],
    })
], AppModule);
export { AppModule };
//# sourceMappingURL=app.module.js.map