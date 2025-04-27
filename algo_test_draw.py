# import pandas as pd
# import random
# import heapq
# import numpy as np
# import time
# import os
# import matplotlib.pyplot as plt
# from matplotlib import animation

# # ================================================
# # 작업 디렉토리 설정
# # ================================================
# os.chdir("C:/")
# os.makedirs("algo_test_results", exist_ok=True)

# # ================================================
# # CSV 데이터 로드
# # ================================================
# def load_data(file_path):
#     return pd.read_csv(file_path)

# data = load_data('2023.csv')

# # ================================================
# # 환경 기반 쓰레기 및 장애물 생성 함수
# # ================================================
# def generate_trash_obstacles_with_environment(grid_size, env_row):
#     wind_speed = env_row['풍속(m/s)']
#     wind_direction = env_row['풍향(deg)']
#     gust_speed = env_row['GUST풍속(m/s)']
#     max_wave_height = env_row['최대파고(m)']
#     significant_wave_height = env_row['유의파고(m)']
#     mean_wave_height = env_row['평균파고(m)']
#     wave_period = env_row['파주기(sec)']
#     wave_direction = env_row['파향(deg)']

#     if pd.isna(wind_speed): wind_speed = 0
#     if pd.isna(wind_direction): wind_direction = 0
#     if pd.isna(gust_speed): gust_speed = 0
#     if pd.isna(max_wave_height): max_wave_height = 0
#     if pd.isna(significant_wave_height): significant_wave_height = 0
#     if pd.isna(mean_wave_height): mean_wave_height = 0
#     if pd.isna(wave_period): wave_period = 0
#     if pd.isna(wave_direction): wave_direction = 0

#     base_trash = 50
#     base_obstacles = 10

#     trash_count = min(base_trash + int(wind_speed * 5) + int(significant_wave_height * 10), 300)
#     obstacle_count = min(base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5), 50)

#     trash_positions = []
#     for _ in range(trash_count):
#         base_x = random.randint(0, grid_size-1)
#         base_y = random.randint(0, grid_size-1)
#         offset_distance = random.randint(0, 5)
#         offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
#         offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)
#         new_x = max(0, min(grid_size-1, base_x + offset_x))
#         new_y = max(0, min(grid_size-1, base_y + offset_y))
#         trash_positions.append((new_x, new_y))

#     obstacle_positions = []
#     for _ in range(obstacle_count):
#         base_x = random.randint(0, grid_size-1)
#         base_y = random.randint(0, grid_size-1)
#         offset_distance = random.randint(0, 3)
#         offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
#         offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)
#         new_x = max(0, min(grid_size-1, base_x + offset_x))
#         new_y = max(0, min(grid_size-1, base_y + offset_y))
#         obstacle_positions.append((new_x, new_y))

#     return trash_positions, obstacle_positions

# # ================================================
# # 알고리즘 함수들
# # ================================================
# def greedy_algorithm(start, trash_positions, obstacle_positions):
#     path = [start]
#     current_position = start

#     while trash_positions:
#         next_position = min(trash_positions, key=lambda p: np.linalg.norm(np.array(p) - np.array(current_position)))
#         path.append(next_position)
#         trash_positions.remove(next_position)
#         current_position = next_position

#     return path

# def dijkstra_algorithm(start, trash_positions, obstacle_positions):
#     path = []
#     grid_size = 50
#     directions = [(-1,0),(1,0),(0,-1),(0,1)]
#     visited = set()
#     pq = [(0, start, [])]

#     while pq and trash_positions:
#         cost, current_position, current_path = heapq.heappop(pq)
#         if current_position in visited:
#             continue
#         visited.add(current_position)
#         current_path = current_path + [current_position]
#         if current_position in trash_positions:
#             trash_positions.remove(current_position)
#             path.extend(current_path)
#             current_path = []
#         for d in directions:
#             neighbor = (current_position[0]+d[0], current_position[1]+d[1])
#             if 0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size:
#                 if neighbor not in visited and neighbor not in obstacle_positions:
#                     heapq.heappush(pq, (cost+1, neighbor, current_path))
#     return path

# def a_star_algorithm(start, trash_positions, obstacle_positions):
#     path = []
#     grid_size = 50
#     directions = [(-1,0),(1,0),(0,-1),(0,1)]
#     open_list = [(0, start, [])]
#     closed_set = set()

#     while open_list and trash_positions:
#         _, current_position, current_path = min(open_list, key=lambda p: p[0])
#         open_list.remove((_, current_position, current_path))
#         if current_position in closed_set:
#             continue
#         closed_set.add(current_position)
#         current_path = current_path + [current_position]
#         if current_position in trash_positions:
#             trash_positions.remove(current_position)
#             path.extend(current_path)
#             current_path = []
#         for d in directions:
#             neighbor = (current_position[0]+d[0], current_position[1]+d[1])
#             if 0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size:
#                 if neighbor not in closed_set and neighbor not in obstacle_positions:
#                     cost = len(current_path) + np.linalg.norm(np.array(neighbor)-np.array(current_position))
#                     open_list.append((cost, neighbor, current_path))
#     return path

# def d_star_algorithm(start, trash_positions, obstacle_positions):
#     grid_size = 50
#     directions = [(-1,0),(1,0),(0,-1),(0,1)]
#     path = []
#     current_position = start

#     def heuristic(a, b):
#         return np.linalg.norm(np.array(a) - np.array(b))

#     def a_star(start, goal, obstacles):
#         open_set = []
#         heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
#         visited = set()

#         while open_set:
#             est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)
#             if current == goal:
#                 return current_path
#             if current in visited:
#                 continue
#             visited.add(current)
#             for d in directions:
#                 neighbor = (current[0]+d[0], current[1]+d[1])
#                 if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
#                     if neighbor not in visited:
#                         new_cost = cost_so_far + 1
#                         est_total = new_cost + heuristic(neighbor, goal)
#                         heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
#         return None

#     while trash_positions:
#         next_target = min(trash_positions, key=lambda p: heuristic(current_position, p))
#         route = a_star(current_position, next_target, set(obstacle_positions))
#         if route is None:
#             trash_positions.remove(next_target)
#             continue
#         path.extend(route[1:])
#         current_position = next_target
#         trash_positions.remove(next_target)

#     return path

# # ================================================
# # 경로 시각화 함수
# # ================================================
# algo_colors = {
#     "❤️Greedy": "pink",
#     "💛Dijkstra": "gold",
#     "💚A-Star": "limegreen",
#     "💙D-Star": "skyblue"
# }

# def visualize_all_paths(grid_size, start, trash_positions, obstacle_positions, paths_dict):
#     fig, axes = plt.subplots(2, 2, figsize=(10, 10))
#     plt.suptitle('Algorithm Path Comparison', fontsize=20)

#     algo_list = list(paths_dict.keys())
#     lines = []

#     for idx, ax in enumerate(axes.flat):
#         if idx >= len(algo_list):
#             ax.axis('off')
#             continue
        
#         algo_name = algo_list[idx]
#         path = paths_dict[algo_name]

#         ax.set_xlim(-1, grid_size)
#         ax.set_ylim(-1, grid_size)
#         ax.set_xticks([])
#         ax.set_yticks([])
#         ax.grid(True)

#         if obstacle_positions:
#             ox, oy = zip(*obstacle_positions)
#             ax.scatter(ox, oy, c='black', marker='X', s=20)

#         if trash_positions:
#             tx, ty = zip(*trash_positions)
#             ax.scatter(tx, ty, c='green', marker='s', s=15)

#         ax.scatter(start[0], start[1], c='blue', marker='o', s=30)

#         color = algo_colors.get(algo_name, 'gray')
#         line, = ax.plot([], [], color=color, lw=2, label=algo_name)
#         lines.append(line)

#         ax.set_title(algo_name, fontsize=12)

#     max_length = max(len(p) for p in paths_dict.values())

#     def init():
#         for line in lines:
#             line.set_data([], [])
#         return lines

#     def update(frame):
#         for idx, line in enumerate(lines):
#             algo_name = algo_list[idx]
#             path = paths_dict[algo_name]
#             if frame < len(path):
#                 x_data = [p[0] for p in path[:frame+1]]
#                 y_data = [p[1] for p in path[:frame+1]]
#                 line.set_data(x_data, y_data)
#         return lines

#     ani = animation.FuncAnimation(fig, update, frames=max_length, init_func=init, blit=True, interval=50)
#     plt.tight_layout(rect=[0, 0, 1, 0.95])
#     plt.show()

# # ================================================
# # 테스트 실행
# # ================================================

# # 예시: 첫 번째 데이터로 테스트
# grid_size = 50
# start = (0, 0)
# env_row = data.iloc[0]
# trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(grid_size, env_row)

# greedy_path = greedy_algorithm(start, trash_positions.copy(), obstacle_positions)
# dijkstra_path = dijkstra_algorithm(start, trash_positions.copy(), obstacle_positions)
# a_star_path = a_star_algorithm(start, trash_positions.copy(), obstacle_positions)
# d_star_path = d_star_algorithm(start, trash_positions.copy(), obstacle_positions)

# paths_dict = {
#     "❤️Greedy": greedy_path,
#     "💛Dijkstra": dijkstra_path,
#     "💚A-Star": a_star_path,
#     "💙D-Star": d_star_path
# }

# visualize_all_paths(grid_size, start, trash_positions, obstacle_positions, paths_dict)










import pandas as pd
import random
import heapq
import numpy as np
import time
import os
import matplotlib.pyplot as plt
from matplotlib import animation

# ================================================
# 작업 디렉토리 설정
# ================================================
os.chdir("C:/")
os.makedirs("algo_test_results", exist_ok=True)

# ================================================
# CSV 데이터 로드
# ================================================
def load_data(file_path):
    return pd.read_csv(file_path)

data = load_data('2023.csv')

# ================================================
# 환경 기반 쓰레기 및 장애물 생성 함수
# ================================================
def generate_trash_obstacles_with_environment(grid_size, env_row):
    wind_speed = env_row['풍속(m/s)']
    wind_direction = env_row['풍향(deg)']
    gust_speed = env_row['GUST풍속(m/s)']
    max_wave_height = env_row['최대파고(m)']
    significant_wave_height = env_row['유의파고(m)']
    mean_wave_height = env_row['평균파고(m)']
    wave_period = env_row['파주기(sec)']
    wave_direction = env_row['파향(deg)']

    if pd.isna(wind_speed): wind_speed = 0
    if pd.isna(wind_direction): wind_direction = 0
    if pd.isna(gust_speed): gust_speed = 0
    if pd.isna(max_wave_height): max_wave_height = 0
    if pd.isna(significant_wave_height): significant_wave_height = 0
    if pd.isna(mean_wave_height): mean_wave_height = 0
    if pd.isna(wave_period): wave_period = 0
    if pd.isna(wave_direction): wave_direction = 0

    base_trash = 50
    base_obstacles = 10

    trash_count = min(base_trash + int(wind_speed * 5) + int(significant_wave_height * 10), 300)
    obstacle_count = min(base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5), 50)

    trash_positions = []
    for _ in range(trash_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)
        offset_distance = random.randint(0, 5)
        offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)
        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))
        trash_positions.append((new_x, new_y))

    obstacle_positions = []
    for _ in range(obstacle_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)
        offset_distance = random.randint(0, 3)
        offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)
        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))
        obstacle_positions.append((new_x, new_y))

    return trash_positions, obstacle_positions

# ================================================
# 알고리즘 함수들
# ================================================

def fusion_algo(start, trash_positions, obstacle_positions):
    grid_size = 50
    directions = [(-1,0), (1,0), (0,-1), (0,1)]
    path = []
    current_pos = start

    def heuristic(a, b):
        return np.linalg.norm(np.array(a) - np.array(b))

    def a_star(start, goal, obstacles):
        open_set = []
        heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
        visited = set()

        while open_set:
            est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

            if current == goal:
                return current_path

            if current in visited:
                continue

            visited.add(current)

            for d in directions:
                neighbor = (current[0] + d[0], current[1] + d[1])
                if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
                    if neighbor not in visited:
                        new_cost = cost_so_far + 1
                        est_total = new_cost + heuristic(neighbor, goal)
                        heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
        return None

    while trash_positions:
        # 가장 가까운 쓰레기 선택 (Greedy)
        next_trash = min(trash_positions, key=lambda p: heuristic(current_pos, p))

        # 경로 탐색 (A*)
        route = a_star(current_pos, next_trash, set(obstacle_positions))

        # 만약 경로를 못 찾으면 Dijkstra 스타일로 brute-force
        if route is None:
            candidates = []
            for trash in trash_positions:
                route_try = a_star(current_pos, trash, set(obstacle_positions))
                if route_try:
                    candidates.append((len(route_try), trash, route_try))
            if candidates:
                candidates.sort()
                _, next_trash, route = candidates[0]
            else:
                trash_positions.remove(next_trash)
                continue

        # 경로 추가
        path.extend(route[1:])
        current_pos = next_trash
        trash_positions.remove(next_trash)

        # 주변 센싱 (D*처럼 재탐색)
        nearby_trashes = [p for p in trash_positions if heuristic(current_pos, p) <= 5]
        if nearby_trashes:
            for p in nearby_trashes:
                route_to_nearby = a_star(current_pos, p, set(obstacle_positions))
                if route_to_nearby:
                    path.extend(route_to_nearby[1:])
                    current_pos = p
                    trash_positions.remove(p)

    return path

def predictive_algo(start, trash_positions, obstacle_positions, lookahead=2):
    """
    Predictive Greedy A* Algorithm
    start: 시작 위치 (x, y)
    trash_positions: 수거해야할 쓰레기 리스트 [(x,y), (x,y), ...]
    obstacle_positions: 장애물 리스트 [(x,y), (x,y), ...]
    lookahead: 몇 단계까지 미리 볼지 (default 2단계)
    """
    from heapq import heappush, heappop
    import numpy as np

    grid_size = 50
    directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    trash_positions = trash_positions.copy()
    obstacle_positions_set = set(obstacle_positions)

    path = [start]
    current_position = start

    def heuristic(a, b):
        # 단순 유클리디언 거리
        return np.linalg.norm(np.array(a) - np.array(b))

    def a_star(start, goal):
        open_set = []
        heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
        closed_set = set()

        while open_set:
            est_total_cost, cost_so_far, current, current_path = heappop(open_set)

            if current == goal:
                return current_path

            if current in closed_set:
                continue

            closed_set.add(current)

            for d in directions:
                neighbor = (current[0] + d[0], current[1] + d[1])
                if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacle_positions_set):
                    if neighbor not in closed_set:
                        new_cost = cost_so_far + 1
                        est_total = new_cost + heuristic(neighbor, goal)
                        heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))

        return None

    while trash_positions:
        candidates = []

        for target in trash_positions:
            total_estimated_cost = 0
            simulated_position = target
            visited = [target]

            # 첫 번째 경로는 실제 A*로 계산
            route = a_star(current_position, target)
            if route is None:
                continue
            total_estimated_cost += len(route)

            # lookahead (앞으로 몇 번 더)
            for _ in range(lookahead - 1):
                # 다음 후보 중 가장 가까운거 탐색
                remaining_trash = [t for t in trash_positions if t not in visited]
                if not remaining_trash:
                    break

                next_target = min(remaining_trash, key=lambda p: heuristic(simulated_position, p))
                total_estimated_cost += heuristic(simulated_position, next_target)
                simulated_position = next_target
                visited.append(next_target)

            candidates.append((total_estimated_cost, target))

        if not candidates:
            break

        # 예상 총 거리가 가장 작은 후보 선택
        _, best_target = min(candidates)

        # 실제 이동
        best_route = a_star(current_position, best_target)
        if best_route:
            path.extend(best_route[1:])  # 첫 점(current_position)은 이미 있으니까 제외
            current_position = best_target
            trash_positions.remove(best_target)

    return path

def score_algo(start, trash_positions, obstacle_positions):
    grid_size = 50
    directions = [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]
    path = []
    current_pos = start

    def heuristic(a, b):
        return np.linalg.norm(np.array(a) - np.array(b))

    def a_star(start, goal, obstacles):
        open_set = []
        heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
        visited = set()

        while open_set:
            est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

            if current == goal:
                return current_path

            if current in visited:
                continue

            visited.add(current)

            for d in directions:
                neighbor = (current[0] + d[0], current[1] + d[1])
                if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
                    if neighbor not in visited:
                        new_cost = cost_so_far + 1
                        est_total = new_cost + heuristic(neighbor, goal)
                        heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))
        return None

    while trash_positions:
        # 방향별 점수 계산
        scores = []
        for d in directions:
            score = 0
            for trash in trash_positions:
                vector = (trash[0] - current_pos[0], trash[1] - current_pos[1])
                dot = d[0]*vector[0] + d[1]*vector[1]
                if dot > 0:  # 해당 방향
                    dist = heuristic(current_pos, trash)
                    score += 1 / (dist + 1e-5)
            scores.append(score)

        # 최고 점수 방향 선택
        best_dir_idx = np.argmax(scores)
        best_dir = directions[best_dir_idx]

        # 해당 방향으로 가장 가까운 쓰레기 찾기
        candidates = [p for p in trash_positions if (p[0] - current_pos[0])*best_dir[0] >=0 and (p[1] - current_pos[1])*best_dir[1] >=0]
        if not candidates:
            candidates = trash_positions
        
        next_trash = min(candidates, key=lambda p: heuristic(current_pos, p))

        # 경로 탐색
        route = a_star(current_pos, next_trash, set(obstacle_positions))

        if route:
            path.extend(route[1:])
            current_pos = next_trash
            trash_positions.remove(next_trash)
        else:
            trash_positions.remove(next_trash)

    return path

# ================================================
# 경로 시각화 함수
# ================================================
algo_colors = {
    "❤️Fusion": "pink",
    "💛Predictive": "gold",
    "💚Score": "limegreen",
}

def visualize_all_paths(grid_size, start, trash_positions, obstacle_positions, paths_dict):
    fig, axes = plt.subplots(2, 2, figsize=(10, 10))
    plt.suptitle('Algorithm Path Comparison', fontsize=20)

    algo_list = list(paths_dict.keys())
    lines = []

    for idx, ax in enumerate(axes.flat):
        if idx >= len(algo_list):
            ax.axis('off')
            continue
        
        algo_name = algo_list[idx]
        path = paths_dict[algo_name]

        ax.set_xlim(-1, grid_size)
        ax.set_ylim(-1, grid_size)
        ax.set_xticks([])
        ax.set_yticks([])
        ax.grid(True)

        if obstacle_positions:
            ox, oy = zip(*obstacle_positions)
            ax.scatter(ox, oy, c='black', marker='X', s=20)

        if trash_positions:
            tx, ty = zip(*trash_positions)
            ax.scatter(tx, ty, c='green', marker='s', s=15)

        ax.scatter(start[0], start[1], c='blue', marker='o', s=30)

        color = algo_colors.get(algo_name, 'gray')
        line, = ax.plot([], [], color=color, lw=2, label=algo_name)
        lines.append(line)

        ax.set_title(algo_name, fontsize=12)

    max_length = max(len(p) for p in paths_dict.values())

    def init():
        for line in lines:
            line.set_data([], [])
        return lines

    def update(frame):
        for idx, line in enumerate(lines):
            algo_name = algo_list[idx]
            path = paths_dict[algo_name]
            if frame < len(path):
                x_data = [p[0] for p in path[:frame+1]]
                y_data = [p[1] for p in path[:frame+1]]
                line.set_data(x_data, y_data)
        return lines

    ani = animation.FuncAnimation(fig, update, frames=max_length, init_func=init, blit=True, interval=50)
    plt.tight_layout(rect=[0, 0, 1, 0.95])
    plt.show()

# ================================================
# 테스트 실행
# ================================================

# 예시: 첫 번째 데이터로 테스트
grid_size = 50
start = (0, 0)
env_row = data.iloc[0]
trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(grid_size, env_row)

fusion_path = fusion_algo(start, trash_positions.copy(), obstacle_positions)
predictive_path = predictive_algo(start, trash_positions.copy(), obstacle_positions)
score_path = score_algo(start, trash_positions.copy(), obstacle_positions)

paths_dict = {
    "❤️Fusion": fusion_path,
    "💛Predictive": predictive_path,
    "💚Score": score_path,
}

visualize_all_paths(grid_size, start, trash_positions, obstacle_positions, paths_dict)