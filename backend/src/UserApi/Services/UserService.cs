using AutoMapper;
using UserApi.Authorization;
using UserApi.Entities;
using UserApi.Helpers;
using UserApi.Models;
using UserApi.Repositories;

namespace UserApi.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtUtils _jwtUtils;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;


    public UserService(IUserRepository userRepository, IJwtUtils jwtUtils, IMapper mapper, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtUtils = jwtUtils;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticateResponse?> Authenticate(AuthenticateRequest model)
    {
        // get user from database
        var user = await _userRepository.GetUserByUsernameAsync(model.Username);

        // return null if user not found
        if (user == null) return null;

        // check if the provided password matches the password in the database and return null if it doesn't
        if (!_passwordHasher.ValidatePassword(model.Password, user.PasswordHash, user.PasswordSalt)) return null;

        // authentication successful so generate jwt token
        var token = _jwtUtils.GenerateJwtToken(user);


        // map user and token to response model with Automapper and return
        return _mapper.Map<AuthenticateResponse>(user, opts => opts.Items["Token"] = token);
    }

    public async Task<ChatGetResponse?> AuthenticateRoom(string password, string roomId)
    {
        // get room from database
        var room = await _userRepository.GetRoomByRoomIdAsync(roomId);

        // return null if room not found
        if (room == null) return null;

        // check if the provided password matches the password in the database and return null if it doesn't
        if (!_passwordHasher.ValidatePassword(password, room.PasswordHash, room.PasswordSalt)) return null;



        // authentication successful so continue
        // map room to response model with Automapper and return
        return _mapper.Map<ChatGetResponse>(room);
    }

    public async Task<CreateUserResponse?> CreateUserAsync(CreateUserRequest userRequest)
    {
        // Hash and salt the password
        (byte[] passwordHash, byte[] passwordSalt) = _passwordHasher.HashPassword(userRequest.Password);

        // Map CreateUserRequest model to User entity with Automapper
        var userEntity = _mapper.Map<User>(userRequest);

        // Assign hashed and salted password to user entity
        userEntity.PasswordHash = passwordHash;
        userEntity.PasswordSalt = passwordSalt;

        // Create user in database
        var createdUser = await _userRepository.CreateUserAsync(userEntity)
            ?? throw new Exception("An error occurred when creating user. Try again later.");

        // Map User entity to CreateUserResponse model with Automapper
        return _mapper.Map<CreateUserResponse>(createdUser);
    }

    public async Task<CreateRoomResponse?> CreateRoomAsync(CreateRoomRequest roomRequest, Guid userId)
    {
        // Hash and salt the password
        (byte[] passwordHash, byte[] passwordSalt) = _passwordHasher.HashPassword(roomRequest.Password);

        // Map CreateRoomRequest model to Room entity with Automapper
        var roomEntity = _mapper.Map<Chatroom>(roomRequest);

        // Assign hashed and salted password to user entity
        roomEntity.PasswordHash = passwordHash;
        roomEntity.PasswordSalt = passwordSalt;

        // Create room in database
        var createdRoom = await _userRepository.CreateRoomAsync(roomEntity, userId)
            ?? throw new Exception("An error occurred when creating the room. Try again later.");

        // Map Room entity to CreateRoomResponse model with Automapper
        return _mapper.Map<CreateRoomResponse>(createdRoom);
    }

    public async Task<CreateChatResponse?> CreateMessageAsync(ChatGetRequest chatRequest, Guid userId, Guid roomId)
    {
        // Map ChatGetRequest model to ChatHistory entity with Automapper
        var messageEntity = _mapper.Map<ChatHistory>(chatRequest);

        messageEntity.UserId = userId.ToString();
        messageEntity.RoomId = roomId.ToString();

        // Create room in database
        var createdRoom = await _userRepository.CreateChatAsync(messageEntity)
            ?? throw new Exception("An error occurred when creating the room. Try again later.");

        // Map Room entity to CreateRoomResponse model with Automapper
        return _mapper.Map<CreateChatResponse>(createdRoom);
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();

        return _mapper.Map<IEnumerable<UserResponse>>(users);
    }

    public async Task<IEnumerable<RoomResponse>> GetAllChatroomsAsync()
    {
        var rooms = await _userRepository.GetAllRoomsAsync();

        return _mapper.Map<IEnumerable<RoomResponse>>(rooms);
    }

    public async Task<IEnumerable<ChatResponse>> GetChatsAsync(string id)
    {
        var chats = await _userRepository.GetChatsAsync(id);

        return _mapper.Map<IEnumerable<ChatResponse>>(chats);
    }

    public async Task<UserResponse?> GetByIdAsync(string id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return _mapper.Map<UserResponse>(user);
    }
}